using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Voyage;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class VoyageReportService : IVoyageReportService
    {
        private static readonly string[] StatutsReservationActifs = { "CONFIRMEE", "CONFIRME", "EN_ATTENTE" };
        private static readonly string[] StatutsReservationBillets = { "CONFIRMEE", "CONFIRME" };

        private readonly CongoTravelDbContext _context;
        private readonly IConfigSocieteRepository _configSocieteRepository;
        private readonly IVoyageReportNotificationService _notificationService;
        private readonly ILogger<VoyageReportService> _logger;

        public VoyageReportService(
            CongoTravelDbContext context,
            IConfigSocieteRepository configSocieteRepository,
            IVoyageReportNotificationService notificationService,
            ILogger<VoyageReportService> logger)
        {
            _context = context;
            _configSocieteRepository = configSocieteRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<VoyageReportOperationResult> ReporterAsync(
            int idVoyage,
            int idSociete,
            int idUtilisateur,
            string? userName,
            ReporterVoyageDto dto,
            CancellationToken cancellationToken = default)
        {
            var voyage = await _context.Voyages
                .Include(v => v.Destination)
                .FirstOrDefaultAsync(v => v.Id == idVoyage, cancellationToken);

            if (voyage == null)
                return VoyageReportOperationResult.Fail(404, $"Voyage {idVoyage} introuvable.");

            if (voyage.IdSociete != idSociete)
                return VoyageReportOperationResult.Fail(403, "Le voyage n'appartient pas à votre société.");

            if (voyage.Statut != true)
                return VoyageReportOperationResult.Fail(409, "Impossible de reporter un voyage inactif.");

            var departActuel = voyage.DateDepart.Date.Add(voyage.HeureDepart);
            if (departActuel <= DateTime.UtcNow)
            {
                return VoyageReportOperationResult.Fail(409,
                    "Impossible de reporter un voyage dont la date et l'heure de départ sont déjà passées.");
            }

            var nouvelleDate = dto.DateDepart.Date;
            var nouvelleHeure = dto.HeureDepart;
            var nouveauDepart = nouvelleDate.Add(nouvelleHeure);

            if (nouveauDepart <= DateTime.UtcNow)
                return VoyageReportOperationResult.Fail(409, "La nouvelle date et heure de départ doivent être strictement dans le futur (UTC).");

            if (voyage.DateDepart.Date == nouvelleDate && voyage.HeureDepart == nouvelleHeure)
                return VoyageReportOperationResult.Fail(409, "La nouvelle date/heure est identique à l'horaire actuel du voyage.");

            var conflitVehicule = await _context.Voyages.AsNoTracking()
                .AnyAsync(v =>
                    v.IdVehicule == voyage.IdVehicule
                    && v.DateDepart == nouvelleDate
                    && v.HeureDepart == nouvelleHeure
                    && v.Id != voyage.Id,
                    cancellationToken);

            if (conflitVehicule)
            {
                return VoyageReportOperationResult.Fail(409,
                    $"Un voyage existe déjà pour le véhicule {voyage.IdVehicule} à la date {nouvelleDate:dd/MM/yyyy} et heure {nouvelleHeure:hh\\:mm}.");
            }

            var nowUtc = DateTime.UtcNow;
            var flexPayHolds = await _context.SiegeHoldsEnAttente
                .AsNoTracking()
                .CountAsync(h => h.IdVoyage == idVoyage && h.ExpireAt > nowUtc, cancellationToken);

            if (flexPayHolds > 0)
            {
                return VoyageReportOperationResult.Fail(409,
                    $"Impossible de reporter : {flexPayHolds} siège(s) en attente de paiement FlexPay sur ce voyage.");
            }

            var reservationIds = await _context.Reservations
                .AsNoTracking()
                .Where(r => r.IdVoyage == idVoyage && r.IdSociete == idSociete && r.Statut
                    && StatutsReservationActifs.Contains(r.StatutReservation))
                .Select(r => r.IdReservation)
                .ToListAsync(cancellationToken);

            var billetsImpactes = await (
                from b in _context.Billets
                join r in _context.Reservations on b.IdReservation equals r.IdReservation
                where b.IdReservation.HasValue
                      && reservationIds.Contains(b.IdReservation.Value)
                      && StatutsReservationBillets.Contains(r.StatutReservation)
                select b).ToListAsync(cancellationToken);

            var billetsUtilises = billetsImpactes.Where(b => b.IsUsed).Select(b => b.IdBillet).ToList();
            if (billetsUtilises.Count > 0 && !dto.ConfirmerAvecBilletsUtilises)
            {
                return VoyageReportOperationResult.Fail(409,
                    $"{billetsUtilises.Count} billet(s) déjà utilisé(s) (embarquement). Confirmez avec confirmerAvecBilletsUtilises=true pour continuer.",
                    billetsUtilises);
            }

            var ancienneDate = voyage.DateDepart;
            var ancienneHeure = voyage.HeureDepart;
            var config = await _configSocieteRepository.GetOrCreateAsync(idSociete, cancellationToken);
            var dureeValidite = config.DureeValiditeBilletJours;
            var billetsRecalcules = 0;

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

                voyage.DateDepart = nouvelleDate;
                voyage.HeureDepart = nouvelleHeure;
                voyage.DateModification = DateTime.UtcNow;

                foreach (var billet in billetsImpactes)
                {
                    if (billet.IsUsed && !dto.ConfirmerAvecBilletsUtilises)
                        continue;

                    BilletValidityHelper.ApplyToBillet(billet, nouvelleDate, dureeValidite);
                    billet.DateModification = DateTime.UtcNow;
                    billetsRecalcules++;
                }

                _context.AuditLogs.Add(new AuditLog
                {
                    TableName = "Voyage",
                    RecordId = voyage.Id,
                    Action = "REPORT",
                    UserId = idUtilisateur,
                    UserName = string.IsNullOrWhiteSpace(userName) ? $"user:{idUtilisateur}" : userName,
                    IdSociete = idSociete,
                    ChangedFields = "DateDepart,HeureDepart",
                    DateAction = DateTime.UtcNow,
                    Success = true,
                    Commentaire = BuildAuditCommentaire(ancienneDate, ancienneHeure, nouvelleDate, nouvelleHeure, dto.Motif, reservationIds.Count, billetsRecalcules)
                });

                await _context.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                var resultDto = new ReporterVoyageResultDto
                {
                    IdVoyage = voyage.Id,
                    AncienneDateDepart = ancienneDate,
                    AncienneHeureDepart = ancienneHeure,
                    NouvelleDateDepart = nouvelleDate,
                    NouvelleHeureDepart = nouvelleHeure,
                    NombreReservationsImpactees = reservationIds.Count,
                    NombreBilletsRecalcules = billetsRecalcules
                };

                if (billetsUtilises.Count > 0 && dto.ConfirmerAvecBilletsUtilises)
                {
                    resultDto.Avertissements.Add(
                        $"{billetsUtilises.Count} billet(s) déjà utilisé(s) : validités recalculées malgré l'embarquement enregistré.");
                }

                if (dto.NotifierClients && reservationIds.Count > 0)
                {
                    try
                    {
                        var (envoyees, echecs) = await _notificationService.NotifyReservedClientsAsync(
                            voyage,
                            ancienneDate,
                            ancienneHeure,
                            dto.Motif,
                            cancellationToken);
                        resultDto.NotificationsEnvoyees = envoyees;
                        resultDto.NotificationsEchecs = echecs;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors des notifications de report voyage {IdVoyage}", idVoyage);
                        resultDto.Avertissements.Add("Notifications client partiellement ou totalement en échec.");
                    }
                }

                _logger.LogInformation(
                    "Voyage {IdVoyage} reporté — {OldDate} {OldTime} → {NewDate} {NewTime}, réservations={Res}, billets={Bil}",
                    idVoyage, ancienneDate.ToString("yyyy-MM-dd"), ancienneHeure, nouvelleDate.ToString("yyyy-MM-dd"), nouvelleHeure,
                    reservationIds.Count, billetsRecalcules);

                return VoyageReportOperationResult.Ok(resultDto);
            });
        }

        private static string BuildAuditCommentaire(
            DateTime ancienneDate,
            TimeSpan ancienneHeure,
            DateTime nouvelleDate,
            TimeSpan nouvelleHeure,
            string? motif,
            int nbReservations,
            int nbBillets)
        {
            var msg = $"Report voyage : {ancienneDate:dd/MM/yyyy} {ancienneHeure:hh\\:mm} → {nouvelleDate:dd/MM/yyyy} {nouvelleHeure:hh\\:mm}. " +
                      $"Réservations impactées={nbReservations}, billets recalculés={nbBillets}.";
            if (!string.IsNullOrWhiteSpace(motif))
                msg += $" Motif: {motif.Trim()}.";
            return msg;
        }
    }
}
