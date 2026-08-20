using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CongoTravel.Configuration;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Models.DTOs.ReversementSite;
using CongoTravel.Models.Enums;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services
{
    public class ReversementSiteService : IReversementSiteService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IFlexPayService _flexPayService;
        private readonly IInfoPaiementResolutionService _infoPaiementResolution;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly FlexPayOptions _flexPayOptions;
        private readonly ILogger<ReversementSiteService> _logger;

        public ReversementSiteService(
            CongoTravelDbContext context,
            IFlexPayService flexPayService,
            IInfoPaiementResolutionService infoPaiementResolution,
            IHttpContextAccessor httpContextAccessor,
            IOptions<FlexPayOptions> flexPayOptions,
            ILogger<ReversementSiteService> logger)
        {
            _context = context;
            _flexPayService = flexPayService;
            _infoPaiementResolution = infoPaiementResolution;
            _httpContextAccessor = httpContextAccessor;
            _flexPayOptions = flexPayOptions.Value;
            _logger = logger;
        }

        public Task<ReversementSiteResponseDto> InitierAsync(
            InitierReversementSiteDto dto,
            int idUtilisateur,
            CancellationToken cancellationToken = default) =>
            InitierCoreAsync(
                dto.IdSite,
                dto.IdSociete,
                idUtilisateur,
                dto.Montant,
                dto.CodeDevise,
                dto.Motif,
                ReversementSiteOrigines.Manuel,
                idPaiement: null,
                idReservation: null,
                modulePaiement: null,
                idPaiementSource: null,
                enforceManualPendingCheck: true,
                throwOnFlexPayFailure: true,
                cancellationToken);

        public Task<ReversementSiteResponseDto?> InitierPourPaiementAsync(
            int idPaiement,
            int idReservation,
            int idSite,
            int idSociete,
            int idUtilisateur,
            decimal montant,
            string codeDevise,
            string? motif,
            CancellationToken cancellationToken = default) =>
            InitierPourPaiementAsync(
                ReversementModulePaiement.Transport,
                idPaiement,
                idReservation,
                idSite,
                idSociete,
                idUtilisateur,
                montant,
                codeDevise,
                motif,
                idPaiementTransport: idPaiement,
                idReservationTransport: idReservation,
                cancellationToken);

        public async Task<ReversementSiteResponseDto?> InitierPourPaiementAsync(
            string modulePaiement,
            int idPaiementSource,
            int? idReservationSource,
            int idSite,
            int idSociete,
            int idUtilisateur,
            decimal montant,
            string codeDevise,
            string? motif,
            int? idPaiementTransport = null,
            int? idReservationTransport = null,
            CancellationToken cancellationToken = default)
        {
            var module = string.IsNullOrWhiteSpace(modulePaiement)
                ? ReversementModulePaiement.Transport
                : modulePaiement.Trim();

            var existing = await FindExistingAutoReversementAsync(
                module, idPaiementSource, idPaiementTransport, cancellationToken);

            if (existing != null)
                return MapToDto(existing);

            var idPaiement = idPaiementTransport
                ?? (string.Equals(module, ReversementModulePaiement.Transport, StringComparison.Ordinal)
                    ? idPaiementSource
                    : (int?)null);
            var idReservation = idReservationTransport
                ?? (string.Equals(module, ReversementModulePaiement.Transport, StringComparison.Ordinal)
                    ? idReservationSource
                    : null);

            try
            {
                return await InitierCoreAsync(
                    idSite,
                    idSociete,
                    idUtilisateur,
                    montant,
                    codeDevise,
                    motif,
                    ReversementSiteOrigines.PaiementElectronique,
                    idPaiement,
                    idReservation,
                    module,
                    idPaiementSource,
                    enforceManualPendingCheck: false,
                    throwOnFlexPayFailure: false,
                    cancellationToken);
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("NumeroMobileMoney", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("Mobile Money", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(ex,
                    "Reversement auto ignoré — site {IdSite} sans NumeroMobileMoney valide (module {Module}, paiementSource {IdPaiementSource})",
                    idSite, module, idPaiementSource);
                return null;
            }
        }

        private async Task<ReversementSite?> FindExistingAutoReversementAsync(
            string modulePaiement,
            int idPaiementSource,
            int? idPaiementTransport,
            CancellationToken cancellationToken)
        {
            var byModule = await _context.ReversementsSite.AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.ModulePaiement == modulePaiement && r.IdPaiementSource == idPaiementSource,
                    cancellationToken);
            if (byModule != null)
                return byModule;

            // Lignes historiques Transport : IdPaiement renseigné, ModulePaiement encore null.
            if (string.Equals(modulePaiement, ReversementModulePaiement.Transport, StringComparison.Ordinal))
            {
                var transportId = idPaiementTransport ?? idPaiementSource;
                return await _context.ReversementsSite.AsNoTracking()
                    .FirstOrDefaultAsync(r => r.IdPaiement == transportId, cancellationToken);
            }

            return null;
        }

        private async Task<ReversementSiteResponseDto> InitierCoreAsync(
            int idSite,
            int idSociete,
            int idUtilisateur,
            decimal montant,
            string codeDeviseInput,
            string? motif,
            string origine,
            int? idPaiement,
            int? idReservation,
            string? modulePaiement,
            int? idPaiementSource,
            bool enforceManualPendingCheck,
            bool throwOnFlexPayFailure,
            CancellationToken cancellationToken)
        {
            if (!_flexPayOptions.Enabled)
                throw new InvalidOperationException("FlexPay est désactivé dans la configuration.");

            await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(
                _context, idSite, idSociete, cancellationToken);

            var site = await _context.Sites.AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdSite == idSite && s.IdSociete == idSociete && s.Statut, cancellationToken)
                ?? throw new InvalidOperationException($"Site {idSite} introuvable ou inactif.");

            if (!MobileMoneyPhoneHelper.TryNormalize(site.NumeroMobileMoney, out var phone, out var phoneError))
                throw new InvalidOperationException(phoneError ?? "NumeroMobileMoney invalide pour ce site.");

            var codeDevise = (codeDeviseInput ?? "CDF").Trim().ToUpperInvariant();
            if (codeDevise is not ("CDF" or "USD"))
                throw new InvalidOperationException("La devise doit être CDF ou USD.");

            if (montant <= 0)
                throw new InvalidOperationException("Le montant doit être strictement positif.");

            if (enforceManualPendingCheck)
            {
                var pendingSince = DateTime.UtcNow.AddMinutes(-Math.Max(1, _flexPayOptions.PayOutPendingMinutes));
                var pendingExists = await _context.ReversementsSite.AsNoTracking()
                    .AnyAsync(r =>
                        r.IdSite == idSite
                        && r.Origine == ReversementSiteOrigines.Manuel
                        && r.Statut == StatutReversementSite.EnAttente
                        && r.DateCreation >= pendingSince,
                        cancellationToken);

                if (pendingExists)
                {
                    throw new InvalidOperationException(
                        $"Un reversement est déjà en attente pour le site {idSite}. Réessayez dans {_flexPayOptions.PayOutPendingMinutes} minutes ou attendez le callback.");
                }
            }

            var infoPaiement = await _infoPaiementResolution.ResolveActiveForSiteAsync(
                idSite, idSociete, cancellationToken);

            if (!infoPaiement.ActifMobileMoney)
                throw new InvalidOperationException("Le Mobile Money n'est pas actif pour la configuration FlexPay de ce site.");

            var reference = GenerateReference(idSite);
            var callbackUrl = FlexPayUrlHelper.ResolvePayOutCallbackUrl(
                _httpContextAccessor.HttpContext,
                _flexPayOptions.CallbackBaseUrl,
                _flexPayOptions.ForceProductionCallbackInDev);

            var reversement = new ReversementSite
            {
                IdSite = idSite,
                IdSociete = idSociete,
                IdUtilisateur = idUtilisateur,
                IdPaiement = idPaiement,
                IdReservation = idReservation,
                ModulePaiement = string.IsNullOrWhiteSpace(modulePaiement) ? null : modulePaiement.Trim(),
                IdPaiementSource = idPaiementSource,
                Origine = origine,
                NumeroMobileMoney = phone,
                Montant = montant,
                CodeDevise = codeDevise,
                Reference = reference,
                CodeMarchand = infoPaiement.CodeMarchand,
                Motif = string.IsNullOrWhiteSpace(motif) ? null : motif.Trim(),
                Statut = StatutReversementSite.EnAttente
            };

            _context.ReversementsSite.Add(reversement);
            await _context.SaveChangesAsync(cancellationToken);

            var flexResponse = await _flexPayService.InitierPayOutAsync(
                infoPaiement.CodeMarchand,
                infoPaiement.ApiToken,
                reference,
                phone,
                montant,
                codeDevise,
                callbackUrl,
                cancellationToken);

            reversement.CodeFlexPay = flexResponse.Code;
            reversement.MessageFlexPay = flexResponse.Message;

            if (!flexResponse.IsSuccess)
            {
                reversement.Statut = StatutReversementSite.Echec;
                await _context.SaveChangesAsync(cancellationToken);

                if (throwOnFlexPayFailure)
                {
                    throw new InvalidOperationException(
                        flexResponse.Message ?? "FlexPay a refusé l'initiation du reversement.");
                }

                _logger.LogWarning(
                    "Reversement auto échoué côté FlexPay — id {IdReversementSite}, paiement {IdPaiement}: {Message}",
                    reversement.IdReversementSite, idPaiement, flexResponse.Message);

                return MapToDto(reversement);
            }

            reversement.OrderNumber = string.IsNullOrWhiteSpace(flexResponse.OrderNumber)
                ? null
                : flexResponse.OrderNumber.Trim();

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Reversement site initié — id {IdReversementSite}, site {IdSite}, origine {Origine}, orderNumber {OrderNumber}",
                reversement.IdReversementSite, reversement.IdSite, reversement.Origine, reversement.OrderNumber);

            return MapToDto(reversement);
        }

        public async Task<ReversementSiteResponseDto?> GetByIdAsync(
            int id,
            int idSociete,
            bool isSuperAdmin,
            CancellationToken cancellationToken = default)
        {
            var query = _context.ReversementsSite.AsNoTracking().Where(r => r.IdReversementSite == id);
            if (!isSuperAdmin)
                query = query.Where(r => r.IdSociete == idSociete);

            var entity = await query.FirstOrDefaultAsync(cancellationToken);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<PagedResponse<ReversementSiteResponseDto>> GetBySitePagedAsync(
            int idSite,
            int idSociete,
            PagedRequest request,
            bool isSuperAdmin,
            CancellationToken cancellationToken = default)
        {
            await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(
                _context, idSite, idSociete, cancellationToken);

            var query = _context.ReversementsSite.AsNoTracking().Where(r => r.IdSite == idSite);
            if (!isSuperAdmin)
                query = query.Where(r => r.IdSociete == idSociete);

            var total = await query.CountAsync(cancellationToken);
            var page = Math.Max(1, request.PageNumber);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);

            var items = await query
                .OrderByDescending(r => r.DateCreation)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResponse<ReversementSiteResponseDto>(
                items.Select(MapToDto).ToList(),
                page,
                pageSize,
                total);
        }

        public async Task<ReversementSiteResponseDto> VerifierEtFinaliserAsync(
            string orderNumber,
            int idSociete,
            bool isSuperAdmin,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                throw new ArgumentException("orderNumber requis.", nameof(orderNumber));

            var reversement = await _context.ReversementsSite
                .FirstOrDefaultAsync(r => r.OrderNumber == orderNumber, cancellationToken)
                ?? throw new InvalidOperationException($"Reversement FlexPay {orderNumber} introuvable.");

            if (!isSuperAdmin && reversement.IdSociete != idSociete)
                throw new InvalidOperationException("Accès refusé à ce reversement.");

            if (reversement.Statut is StatutReversementSite.Succes or StatutReversementSite.Echec or StatutReversementSite.Annule)
                return MapToDto(reversement);

            var infoPaiement = await _infoPaiementResolution.ResolveActiveForSiteAsync(
                reversement.IdSite, reversement.IdSociete, cancellationToken);

            var check = await _flexPayService.VerifierStatutTransactionAsync(
                infoPaiement.ApiToken, orderNumber, cancellationToken);

            var status = check.Transaction?.Status ?? check.Code;
            if (FlexPayStatusHelper.ShouldTreatAsPending(status))
            {
                reversement.MessageFlexPay = check.Message ?? "Transaction en attente chez FlexPay.";
                reversement.CodeFlexPay = status;
                await _context.SaveChangesAsync(cancellationToken);
                return MapToDto(reversement);
            }

            var callback = new Models.DTOs.FlexPay.FlexPayCallbackDto
            {
                Code = FlexPayStatusHelper.IsSuccess(status) ? "0" : "1",
                Reference = reversement.Reference,
                OrderNumber = orderNumber,
                Channel = reversement.Channel
            };

            FlexPayPayOutCallbackService.ApplyCallbackToReversement(reversement, callback);
            reversement.DateCallback = DateTime.UtcNow;
            reversement.MessageFlexPay = check.Message ?? reversement.MessageFlexPay;
            reversement.CodeFlexPay = status;

            await _context.SaveChangesAsync(cancellationToken);
            return MapToDto(reversement);
        }

        private static string GenerateReference(int idSite)
        {
            var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            var reference = $"REV{idSite}{suffix}";
            return reference.Length <= 20 ? reference : reference[..20];
        }

        private static ReversementSiteResponseDto MapToDto(ReversementSite entity) => new()
        {
            IdReversementSite = entity.IdReversementSite,
            IdPaiement = entity.IdPaiement,
            IdReservation = entity.IdReservation,
            ModulePaiement = entity.ModulePaiement,
            IdPaiementSource = entity.IdPaiementSource,
            Origine = entity.Origine,
            IdSite = entity.IdSite,
            IdSociete = entity.IdSociete,
            IdUtilisateur = entity.IdUtilisateur,
            NumeroMobileMoney = entity.NumeroMobileMoney,
            Montant = entity.Montant,
            CodeDevise = entity.CodeDevise,
            Reference = entity.Reference,
            OrderNumber = entity.OrderNumber,
            ProviderReference = entity.ProviderReference,
            CodeMarchand = entity.CodeMarchand,
            Statut = entity.Statut,
            CodeFlexPay = entity.CodeFlexPay,
            MessageFlexPay = entity.MessageFlexPay,
            Channel = entity.Channel,
            Motif = entity.Motif,
            DateCreation = entity.DateCreation,
            DateCallback = entity.DateCallback
        };
    }
}
