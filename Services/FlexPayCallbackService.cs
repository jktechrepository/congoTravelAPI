using System.Globalization;
using System.Text.Json;
using CongoTravel.Configuration;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.DTOs.Reservation;
using CongoTravel.Models.Enums;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CongoTravel.Services
{
    public class FlexPayCallbackService : IFlexPayCallbackService
    {
        private const decimal MontantTolerance = 0.05m;

        private readonly CongoTravelDbContext _context;
        private readonly ISiegeDisponibiliteService _siegeDisponibilite;
        private readonly BilletEmissionService _billetEmissionService;
        private readonly IFlexPayService _flexPayService;
        private readonly IFlexPayRealtimeNotifier _flexPayRealtimeNotifier;
        private readonly IReservationWithPaiementReadService _reservationWithPaiementReadService;
        private readonly IInfoPaiementResolutionService _infoPaiementResolution;
        private readonly IReversementAutomatiqueService _reversementAutomatiqueService;
        private readonly FlexPayOptions _flexPayOptions;
        private readonly ILogger<FlexPayCallbackService> _logger;

        public FlexPayCallbackService(
            CongoTravelDbContext context,
            ISiegeDisponibiliteService siegeDisponibilite,
            BilletEmissionService billetEmissionService,
            IFlexPayService flexPayService,
            IFlexPayRealtimeNotifier flexPayRealtimeNotifier,
            IReservationWithPaiementReadService reservationWithPaiementReadService,
            IOptions<FlexPayOptions> flexPayOptions,
            IInfoPaiementResolutionService infoPaiementResolution,
            IReversementAutomatiqueService reversementAutomatiqueService,
            ILogger<FlexPayCallbackService> logger)
        {
            _context = context;
            _siegeDisponibilite = siegeDisponibilite;
            _billetEmissionService = billetEmissionService;
            _flexPayService = flexPayService;
            _flexPayRealtimeNotifier = flexPayRealtimeNotifier;
            _reservationWithPaiementReadService = reservationWithPaiementReadService;
            _flexPayOptions = flexPayOptions.Value;
            _infoPaiementResolution = infoPaiementResolution;
            _reversementAutomatiqueService = reversementAutomatiqueService;
            _logger = logger;
        }

        public async Task<FlexPayCallbackProcessResultDto> ProcessCallbackAsync(
            FlexPayCallbackDto callback,
            string? payloadComplet,
            string? headers,
            string? ipSource,
            CancellationToken cancellationToken = default)
        {
            var audit = new CallbackFlexPay
            {
                OrderNumber = callback.OrderNumber,
                Code = callback.Code,
                Reference = callback.Reference,
                ProviderReference = callback.ProviderReference,
                Amount = callback.Amount,
                AmountCustomer = callback.AmountCustomer,
                Phone = callback.Phone,
                Currency = callback.Currency,
                Channel = callback.Channel,
                CreatedAt = callback.CreatedAt,
                PayloadComplet = payloadComplet,
                Headers = headers,
                IpSource = ipSource
            };

            TransactionFlexPay? transaction = null;
            try
            {
                if (string.IsNullOrWhiteSpace(callback.OrderNumber) && string.IsNullOrWhiteSpace(callback.Reference))
                {
                    audit.MessageErreur = "OrderNumber ou Reference requis.";
                    return await SaveAuditAndReturnAsync(audit, false, audit.MessageErreur, cancellationToken);
                }

                transaction = await FindTransactionAsync(callback, cancellationToken);
                audit.IdTransaction = transaction?.IdTransaction;

                var earlyIdempotent = await TryGetIdempotentResultAsync(transaction, callback, cancellationToken);
                if (earlyIdempotent != null)
                {
                    audit.TraiteAvecSucces = true;
                    audit.DetailsTraitement = earlyIdempotent.Message;
                    await PersistAuditAsync(audit, cancellationToken);
                    return earlyIdempotent;
                }

                var commande = await FindCommandeAsync(callback, transaction, cancellationToken);
                if (commande == null)
                {
                    audit.MessageErreur = "Commande en attente introuvable.";
                    return await SaveAuditAndReturnAsync(audit, false, audit.MessageErreur, cancellationToken);
                }

                if (transaction == null)
                {
                    transaction = await _context.TransactionsFlexPay
                        .FirstOrDefaultAsync(t => t.IdCommandeReservationEnAttente == commande.IdCommandeReservationEnAttente, cancellationToken);
                }

                var paiement = commande.IdPaiementEnAttente.HasValue
                    ? await _context.Paiements.FindAsync(new object[] { commande.IdPaiementEnAttente.Value }, cancellationToken)
                    : null;

                if (paiement?.Statut == true && paiement.IdReservation.HasValue)
                {
                    audit.TraiteAvecSucces = true;
                    audit.DetailsTraitement = "Déjà finalisé (idempotence).";
                    await PersistAuditAsync(audit, cancellationToken);
                    return new FlexPayCallbackProcessResultDto
                    {
                        Success = true,
                        AlreadyProcessed = true,
                        Message = audit.DetailsTraitement,
                        IdReservation = paiement.IdReservation,
                        IdPaiement = paiement.IdPaiement
                    };
                }

                if (transaction != null)
                {
                    transaction.NombreCallbacks++;
                    transaction.DateCallback = DateTime.UtcNow;
                    transaction.ProviderReference = callback.ProviderReference ?? transaction.ProviderReference;
                    transaction.Channel = callback.Channel ?? transaction.Channel;
                    transaction.CodeFlexPay = callback.Code;
                }

                var success = callback.Code == "0";
                if (!success)
                {
                    await MarkFailureAsync(commande, paiement, transaction, cancellationToken);
                    audit.TraiteAvecSucces = true;
                    audit.DetailsTraitement = "Paiement refusé par FlexPay.";
                    await PersistAuditAsync(audit, cancellationToken);
                    if (transaction != null)
                        await _context.SaveChangesAsync(cancellationToken);

                    await TryNotifyPaymentFailedAsync(commande, callback.OrderNumber, audit.DetailsTraitement, cancellationToken);

                    return new FlexPayCallbackProcessResultDto
                    {
                        Success = true,
                        Message = audit.DetailsTraitement
                    };
                }

                ValidateCallbackAmount(callback, commande);
                FlexPayCurrencyPolicy.EnsureCallbackCurrencyMatchesExpected(
                    callback.Currency,
                    commande.CodeDevisePaiement,
                    "Callback FlexPay transport");

                var result = await FinalizeSuccessAsync(commande, paiement, transaction, callback, cancellationToken);
                audit.TraiteAvecSucces = true;
                audit.DetailsTraitement = result.Message;
                await PersistAuditAsync(audit, cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur traitement callback FlexPay");
                audit.MessageErreur = ex.Message;
                audit.TraiteAvecSucces = false;
                await PersistAuditAsync(audit, cancellationToken);
                if (transaction != null)
                    await _context.SaveChangesAsync(cancellationToken);

                return new FlexPayCallbackProcessResultDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<FlexPayVerifierResultDto> VerifyAndFinalizeAsync(
            string orderNumber,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                throw new ArgumentException("orderNumber requis.", nameof(orderNumber));

            var transaction = await _context.TransactionsFlexPay
                .FirstOrDefaultAsync(t => t.OrderNumber == orderNumber, cancellationToken)
                ?? throw new InvalidOperationException($"Transaction FlexPay {orderNumber} introuvable.");

            if (transaction.IdReservation.HasValue && transaction.IdPaiement.HasValue)
            {
                var paiementFinalise = await _context.Paiements.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdPaiement == transaction.IdPaiement.Value, cancellationToken);
                if (paiementFinalise?.Statut == true && paiementFinalise.IdReservation.HasValue)
                {
                    return await WrapVerifierResultAsync(
                        new FlexPayCallbackProcessResultDto
                        {
                            Success = true,
                            AlreadyProcessed = true,
                            Message = "Déjà finalisé (idempotence).",
                            IdReservation = paiementFinalise.IdReservation,
                            IdPaiement = paiementFinalise.IdPaiement
                        },
                        orderNumber,
                        cancellationToken);
                }
            }

            var commande = await _context.CommandesReservationEnAttente
                .FirstOrDefaultAsync(c => c.IdCommandeReservationEnAttente == transaction.IdCommandeReservationEnAttente, cancellationToken);

            if (commande == null)
                throw new InvalidOperationException("Commande en attente introuvable.");

            var info = await GetInfoPaiementForSiteAsync(commande.IdSite, commande.IdSociete, cancellationToken);
            var check = await _flexPayService.VerifierStatutTransactionAsync(info.ApiToken, orderNumber, cancellationToken);
            transaction.NombreVerifications++;
            transaction.DateDerniereVerification = DateTime.UtcNow;

            var status = check.Transaction?.Status ?? check.Code;
            if (!string.IsNullOrWhiteSpace(check.Message))
                transaction.MessageFlexPay = check.Message;
            transaction.CodeFlexPay = status;
            transaction.ReponseBruteFlexPay = JsonSerializer.Serialize(check);

            if (FlexPayStatusHelper.ShouldTreatAsPending(status))
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "FlexPay verifier {OrderNumber} : statut {Status} — paiement toujours en attente.",
                    orderNumber, status ?? "(null)");

                return new FlexPayVerifierResultDto
                {
                    StatusOnly = new FlexPayCallbackProcessResultDto
                    {
                        Success = true,
                        AlreadyProcessed = false,
                        PaymentPending = true,
                        Message = "Paiement en attente de validation Mobile Money.",
                        IdReservation = null,
                        IdPaiement = commande.IdPaiementEnAttente
                    }
                };
            }

            var callbackCode = FlexPayStatusHelper.IsSuccess(status) ? "0" : "1";
            var callback = new FlexPayCallbackDto
            {
                Code = callbackCode,
                OrderNumber = orderNumber,
                Reference = transaction.Reference,
                ProviderReference = transaction.ProviderReference,
                Amount = transaction.Amount.ToString(CultureInfo.InvariantCulture),
                Currency = transaction.Currency
            };

            await _context.SaveChangesAsync(cancellationToken);
            var processResult = await ProcessCallbackAsync(callback, JsonSerializer.Serialize(check), null, null, cancellationToken);
            return await WrapVerifierResultAsync(processResult, orderNumber, cancellationToken);
        }

        private async Task<FlexPayVerifierResultDto> WrapVerifierResultAsync(
            FlexPayCallbackProcessResultDto status,
            string orderNumber,
            CancellationToken cancellationToken)
        {
            if (status.Success
                && status.IdReservation.HasValue
                && !status.PaymentPending
                && status.IdReservation.Value > 0)
            {
                var message = status.AlreadyProcessed
                    ? "Déjà finalisé (idempotence)."
                    : status.Message;

                var unified = await _reservationWithPaiementReadService.BuildByReservationIdAsync(
                    status.IdReservation.Value,
                    orderNumber,
                    message,
                    cancellationToken);

                if (unified != null)
                    return new FlexPayVerifierResultDto { ReservationWithPaiement = unified };
            }

            return new FlexPayVerifierResultDto { StatusOnly = status };
        }

        private async Task<FlexPayCallbackProcessResultDto> FinalizeSuccessAsync(
            CommandeReservationEnAttente commande,
            Paiement? paiement,
            TransactionFlexPay? transaction,
            FlexPayCallbackDto callback,
            CancellationToken cancellationToken)
        {
            var dto = JsonSerializer.Deserialize<InitiateFlexPayReservationDto>(commande.PayloadMetierJson)
                      ?? throw new InvalidOperationException("Payload métier invalide.");

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                var useTx = _context.Database.IsRelational();
                await using var tx = useTx
                    ? await _context.Database.BeginTransactionAsync(cancellationToken)
                    : null;
                try
                {
                    var reservation = new Reservation
                    {
                        IdVoyage = dto.Reservation.IdVoyage,
                        IdClient = dto.Reservation.IdClient,
                        IdUtilisateur = dto.Reservation.IdUtilisateur > 0 ? dto.Reservation.IdUtilisateur : commande.IdUtilisateur,
                        IdSociete = dto.Reservation.IdSociete > 0 ? dto.Reservation.IdSociete : commande.IdSociete,
                        IdSite = dto.Reservation.IdSite ?? commande.IdSite,
                        NombreDePlace = dto.Reservation.NombreDePlace,
                        DateReservation = DateTime.UtcNow,
                        StatutReservation = "CONFIRMEE",
                        Statut = true,
                        Origine = commande.Origine
                    };
                    _context.Reservations.Add(reservation);
                    await _context.SaveChangesAsync(cancellationToken);

                    var passengerIds = await CreatePassengersAsync(dto.Reservation, reservation, cancellationToken);
                    await _siegeDisponibilite.ConfirmHoldsAsAllocationsAsync(
                        commande.IdCommandeReservationEnAttente,
                        dto.Reservation.IdVoyage,
                        passengerIds,
                        cancellationToken);

                    if (paiement == null)
                        throw new InvalidOperationException("Paiement en attente introuvable.");

                    paiement.IdReservation = reservation.IdReservation;
                    paiement.MontantPaye = commande.MontantFlexPay;
                    paiement.MontantPayeDevisePrincipale = commande.MontantVoyage;
                    paiement.Statut = true;
                    paiement.StatutPaiementMetier = (int)StatutPaiementMetier.Reussi;
                    paiement.ReferenceTransaction = callback.OrderNumber ?? commande.OrderNumberFlexPay;
                    paiement.DatePaiement = DateTime.UtcNow;
                    paiement.MettreAJourResteAPaye();

                    if (transaction != null)
                    {
                        transaction.StatutPaiement = (int)StatutPaiementMetier.Reussi;
                        transaction.StatusFlexPay = 0;
                        transaction.IdReservation = reservation.IdReservation;
                        transaction.IdPaiement = paiement.IdPaiement;
                        transaction.ProviderReference = callback.ProviderReference ?? transaction.ProviderReference;
                    }

                    _context.CommandesReservationEnAttente.Remove(commande);
                    await _context.SaveChangesAsync(cancellationToken);
                    if (tx != null)
                        await tx.CommitAsync(cancellationToken);

                    try
                    {
                        var billets = await _billetEmissionService.EmitBilletsPourPaiementAsync(paiement);
                        if (billets.Count > 0)
                        {
                            paiement.DateEmissionBillet = DateTime.UtcNow;
                            paiement.IdBilletEmis = billets[0].IdBillet;
                            await _context.SaveChangesAsync(cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Paiement FlexPay confirmé mais émission billet(s) échouée — Réservation {ReservationId}",
                            reservation.IdReservation);
                    }

                    await TryNotifyPaymentConfirmedAsync(
                        commande,
                        paiement,
                        reservation.IdReservation,
                        callback.OrderNumber ?? commande.OrderNumberFlexPay,
                        cancellationToken);

                    await _reversementAutomatiqueService.TryDeclencherApresPaiementElectroniqueAsync(
                        paiement,
                        reservation,
                        cancellationToken);

                    return new FlexPayCallbackProcessResultDto
                    {
                        Success = true,
                        Message = "Réservation créée après confirmation FlexPay.",
                        IdReservation = reservation.IdReservation,
                        IdPaiement = paiement.IdPaiement
                    };
                }
                catch
                {
                    if (tx != null)
                        await tx.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }

        private async Task TryNotifyPaymentConfirmedAsync(
            CommandeReservationEnAttente commande,
            Paiement paiement,
            int idReservation,
            string? orderNumber,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = commande.IdUtilisateur > 0 ? commande.IdUtilisateur : paiement.IdUtilisateur;
                if (userId <= 0 || string.IsNullOrWhiteSpace(orderNumber))
                    return;

                await _flexPayRealtimeNotifier.NotifyPaymentConfirmedAsync(
                    userId,
                    orderNumber,
                    idReservation,
                    paiement.IdPaiement,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR FlexPayPaymentConfirmed non envoyé pour order {OrderNumber}", orderNumber);
            }
        }

        private async Task TryNotifyPaymentFailedAsync(
            CommandeReservationEnAttente commande,
            string? orderNumber,
            string message,
            CancellationToken cancellationToken)
        {
            try
            {
                if (commande.IdUtilisateur <= 0 || string.IsNullOrWhiteSpace(orderNumber))
                    return;

                await _flexPayRealtimeNotifier.NotifyPaymentFailedAsync(
                    commande.IdUtilisateur,
                    orderNumber,
                    message,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR FlexPayPaymentFailed non envoyé pour order {OrderNumber}", orderNumber);
            }
        }

        private async Task<List<int>> CreatePassengersAsync(
            ReservationDataDto data,
            Reservation reservation,
            CancellationToken cancellationToken)
        {
            if (data.Passagers == null || data.Passagers.Count == 0)
                throw new InvalidOperationException("Passagers requis dans le payload.");

            var utcNow = DateTime.UtcNow;
            var added = new List<ReservationPassenger>();
            foreach (var p in data.Passagers)
            {
                var rp = new ReservationPassenger
                {
                    IdReservation = reservation.IdReservation,
                    IdClient = p.IdClient,
                    NomComplet = p.NomComplet.Trim(),
                    Telephone = string.IsNullOrWhiteSpace(p.Telephone) ? null : p.Telephone.Trim(),
                    Email = string.IsNullOrWhiteSpace(p.Email) ? null : p.Email.Trim(),
                    DocumentType = string.IsNullOrWhiteSpace(p.DocumentType) ? null : p.DocumentType.Trim(),
                    DocumentNumero = string.IsNullOrWhiteSpace(p.DocumentNumero) ? null : p.DocumentNumero.Trim(),
                    Genre = string.IsNullOrWhiteSpace(p.Genre) ? null : p.Genre.Trim(),
                    IdSociete = reservation.IdSociete,
                    Statut = true,
                    DateCreation = utcNow
                };
                added.Add(rp);
                _context.ReservationPassengers.Add(rp);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return added.OrderBy(p => p.IdReservationPassenger).Select(p => p.IdReservationPassenger).ToList();
        }

        private async Task MarkFailureAsync(
            CommandeReservationEnAttente commande,
            Paiement? paiement,
            TransactionFlexPay? transaction,
            CancellationToken cancellationToken)
        {
            await _siegeDisponibilite.ReleaseHoldsForCommandeAsync(commande.IdCommandeReservationEnAttente, cancellationToken);

            if (paiement != null)
            {
                paiement.Statut = false;
                paiement.StatutPaiementMetier = (int)StatutPaiementMetier.Echec;
                paiement.MettreAJourResteAPaye();
            }

            if (transaction != null)
            {
                transaction.StatutPaiement = (int)StatutPaiementMetier.Echec;
                transaction.StatusFlexPay = 1;
            }

            _context.CommandesReservationEnAttente.Remove(commande);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static void ValidateCallbackAmount(FlexPayCallbackDto callback, CommandeReservationEnAttente commande)
        {
            if (!decimal.TryParse(callback.Amount, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount)
                && !decimal.TryParse(callback.Amount, NumberStyles.Any, CultureInfo.CurrentCulture, out amount))
            {
                return;
            }

            if (Math.Abs(amount - commande.MontantFlexPay) > MontantTolerance)
            {
                throw new InvalidOperationException(
                    $"Montant callback ({amount}) différent du montant attendu ({commande.MontantFlexPay}).");
            }
        }

        private async Task<FlexPayCallbackProcessResultDto?> TryGetIdempotentResultAsync(
            TransactionFlexPay? transaction,
            FlexPayCallbackDto callback,
            CancellationToken cancellationToken)
        {
            Paiement? paiement = null;
            if (transaction?.IdPaiement != null)
            {
                paiement = await _context.Paiements.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdPaiement == transaction.IdPaiement, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(callback.OrderNumber))
            {
                paiement = await _context.Paiements.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ReferenceTransaction == callback.OrderNumber, cancellationToken);
            }

            if (paiement?.Statut == true && paiement.IdReservation.HasValue)
            {
                return new FlexPayCallbackProcessResultDto
                {
                    Success = true,
                    AlreadyProcessed = true,
                    Message = "Déjà finalisé (idempotence).",
                    IdReservation = paiement.IdReservation,
                    IdPaiement = paiement.IdPaiement
                };
            }

            return null;
        }

        private async Task<TransactionFlexPay?> FindTransactionAsync(
            FlexPayCallbackDto callback,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(callback.OrderNumber))
            {
                return await _context.TransactionsFlexPay
                    .FirstOrDefaultAsync(t => t.OrderNumber == callback.OrderNumber, cancellationToken);
            }

            return null;
        }

        private async Task<CommandeReservationEnAttente?> FindCommandeAsync(
            FlexPayCallbackDto callback,
            TransactionFlexPay? transaction,
            CancellationToken cancellationToken)
        {
            if (transaction?.IdCommandeReservationEnAttente != null)
            {
                return await _context.CommandesReservationEnAttente
                    .FirstOrDefaultAsync(c => c.IdCommandeReservationEnAttente == transaction.IdCommandeReservationEnAttente, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(callback.OrderNumber))
            {
                var byOrder = await _context.CommandesReservationEnAttente
                    .FirstOrDefaultAsync(c => c.OrderNumberFlexPay == callback.OrderNumber, cancellationToken);
                if (byOrder != null)
                    return byOrder;
            }

            if (!string.IsNullOrWhiteSpace(callback.Reference))
            {
                return await _context.CommandesReservationEnAttente
                    .FirstOrDefaultAsync(c => c.ReferenceFlexPay == callback.Reference, cancellationToken);
            }

            return null;
        }

        private async Task<InfoPaiementSociete> GetInfoPaiementForSiteAsync(
            int? idSite,
            int idSociete,
            CancellationToken cancellationToken)
        {
            if (!idSite.HasValue)
                throw new InvalidOperationException("IdSite requis pour la configuration FlexPay.");

            return await _infoPaiementResolution.ResolveActiveForSiteAsync(
                idSite.Value, idSociete, cancellationToken);
        }

        private async Task<FlexPayCallbackProcessResultDto> SaveAuditAndReturnAsync(
            CallbackFlexPay audit,
            bool traite,
            string message,
            CancellationToken cancellationToken)
        {
            audit.TraiteAvecSucces = traite;
            audit.MessageErreur = traite ? null : message;
            audit.DetailsTraitement = message;
            await PersistAuditAsync(audit, cancellationToken);
            return new FlexPayCallbackProcessResultDto { Success = traite, Message = message };
        }

        private async Task PersistAuditAsync(CallbackFlexPay audit, CancellationToken cancellationToken)
        {
            _context.CallbacksFlexPay.Add(audit);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
