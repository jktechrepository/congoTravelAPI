using System.Globalization;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class VoyageService : IVoyageRepository
    {

        private readonly CongoTravelDbContext _context;
        private readonly ILogger<VoyageService> _logger;
        private readonly IVoyageTarifService _voyageTarifService;
        private readonly ISiegeDisponibiliteService _siegeDisponibilite;

        public VoyageService(
            CongoTravelDbContext context,
            ILogger<VoyageService> logger,
            IVoyageTarifService voyageTarifService,
            ISiegeDisponibiliteService siegeDisponibilite)
        {
            _context = context;
            _logger = logger;
            _voyageTarifService = voyageTarifService;
            _siegeDisponibilite = siegeDisponibilite;
        }

        private static IQueryable<Voyage> ApplyDateDepartRange(
            IQueryable<Voyage> query,
            DateTime? dateDepartDebut,
            DateTime? dateDepartFin)
        {
            if (!dateDepartDebut.HasValue || !dateDepartFin.HasValue)
                return query;

            var debut = dateDepartDebut.Value.Date;
            var fin = dateDepartFin.Value.Date;
            return query.Where(v => v.DateDepart.Date >= debut && v.DateDepart.Date <= fin);
        }

        private static IQueryable<Voyage> OrderVoyagesForListe(
            IQueryable<Voyage> query,
            bool filtreDateActif)
        {
            return filtreDateActif
                ? query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart)
                : query.OrderByDescending(v => v.DateCreation);
        }

        /// <summary>Charge type, société et photos du véhicule lié (réponses API voyage).</summary>
        private static IQueryable<Voyage> IncludeVehiculeNavigations(IQueryable<Voyage> query) =>
            query
                .Include(v => v.Vehicule)
                    .ThenInclude(vh => vh!.TypeVehicule)
                .Include(v => v.Vehicule)
                    .ThenInclude(vh => vh!.Societe)
                .Include(v => v.Vehicule)
                    .ThenInclude(vh => vh!.Photos);

        /// <summary>Variante sans société (requêtes qui n'incluaient pas Societe auparavant).</summary>
        private static IQueryable<Voyage> IncludeVehiculeNavigationsLite(IQueryable<Voyage> query) =>
            query
                .Include(v => v.Vehicule)
                    .ThenInclude(vh => vh!.TypeVehicule)
                .Include(v => v.Vehicule)
                    .ThenInclude(vh => vh!.Photos);

        private static IQueryable<Voyage> ApplyActiveSocieteFilter(IQueryable<Voyage> query) =>
            query.Where(v => v.Societe != null && v.Societe.Statut == true);

        private IQueryable<Voyage> BuildVoyageReadQuery(bool publicOnly)
        {
            IQueryable<Voyage> query = _context.Voyages;
            if (publicOnly)
                query = ApplyActiveSocieteFilter(query);

            return IncludeVehiculeNavigations(query)
                .Include(v => v.Destination)
                .Include(v => v.Site)
                .Include(v => v.VoyageTarifsCategorieSiege)
                    .ThenInclude(t => t.CategorieSiege);
        }

        private IQueryable<Voyage> BuildVoyageDetailQuery(bool publicOnly) =>
            BuildVoyageReadQuery(publicOnly)
                .Include(v => v.VoyageDestinations!)
                    .ThenInclude(vd => vd.Destination);

        /// <summary>
        /// Filtre recherche traduisible en SQL (évite <c>ToString(format)</c> non supporté par EF Core / MySQL).
        /// </summary>
        private static IQueryable<Voyage> ApplyVoyageSearchTerm(IQueryable<Voyage> query, string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return query;

            var term = searchTerm.Trim();
            var hasPrix = int.TryParse(term, NumberStyles.Integer, CultureInfo.InvariantCulture, out var prix);

            DateTime? dateDepartDebut = null;
            DateTime? dateDepartFinExcl = null;
            if (DateTime.TryParse(term, CultureInfo.GetCultureInfo("fr-FR"), DateTimeStyles.None, out var dateFr))
            {
                dateDepartDebut = dateFr.Date;
                dateDepartFinExcl = dateDepartDebut.Value.AddDays(1);
            }
            else if (DateTime.TryParse(term, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateInv))
            {
                dateDepartDebut = dateInv.Date;
                dateDepartFinExcl = dateDepartDebut.Value.AddDays(1);
            }

            var hasHeure = TimeSpan.TryParse(term, CultureInfo.InvariantCulture, out var heureDepart);

            return query.Where(v =>
                (v.Destination != null && v.Destination.VilleDepart.Contains(term)) ||
                (v.Destination != null && v.Destination.VilleArrivee.Contains(term)) ||
                (v.Vehicule != null && v.Vehicule.AliasVehicule.Contains(term)) ||
                (hasPrix && v.Prix == prix) ||
                (dateDepartDebut.HasValue && v.DateDepart >= dateDepartDebut.Value && v.DateDepart < dateDepartFinExcl!.Value) ||
                (hasHeure && v.HeureDepart == heureDepart));
        }

        private static string? NormalizeSearchText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim().ToLowerInvariant();
        }

        // CRUD de base
        public async Task<IEnumerable<Voyage>> GetAllAsync(DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null)
        {
            try
            {
                var filtreDateActif = dateDepartDebut.HasValue && dateDepartFin.HasValue;
                var query = ApplyDateDepartRange(
                        IncludeVehiculeNavigations(_context.Voyages)
                            .Include(v => v.Destination)
                            .Include(v => v.Site)
                            .Include(v => v.VoyageTarifsCategorieSiege)
                                .ThenInclude(t => t.CategorieSiege),
                        dateDepartDebut,
                        dateDepartFin);

                return await OrderVoyagesForListe(query, filtreDateActif).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de tous les voyages");
                throw;
            }
        }

        public async Task<IEnumerable<Voyage>> GetAllPublicAsync(DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null)
        {
            try
            {
                var filtreDateActif = dateDepartDebut.HasValue && dateDepartFin.HasValue;
                var query = ApplyDateDepartRange(
                    BuildVoyageReadQuery(publicOnly: true),
                    dateDepartDebut,
                    dateDepartFin);

                return await OrderVoyagesForListe(query, filtreDateActif).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération publique de tous les voyages");
                throw;
            }
        }

        public async Task<Voyage?> GetByIdAsync(int id)
        {
            try
            {
                return await IncludeVehiculeNavigations(_context.Voyages)
                    .Include(v => v.Destination)
                    .Include(v => v.Site)
                    .Include(v => v.VoyageDestinations!)
                        .ThenInclude(vd => vd.Destination)
                    .Include(v => v.VoyageTarifsCategorieSiege)
                        .ThenInclude(t => t.CategorieSiege)
                    .FirstOrDefaultAsync(v => v.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du voyage {VoyageId}", id);
                throw;
            }
        }

        public async Task<Voyage?> GetByIdPublicAsync(int id)
        {
            try
            {
                return await BuildVoyageDetailQuery(publicOnly: true)
                    .FirstOrDefaultAsync(v => v.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération publique du voyage {VoyageId}", id);
                throw;
            }
        }

        public async Task<Voyage> CreateAsync(Voyage voyage, IReadOnlyList<CreateVoyageEtapeDto>? etapesDestinations = null)
        {
            var result = await CreateCoreAsync(voyage, etapesDestinations, new VoyageCreateOptions { ThrowOnConflict = true });
            if (result.Outcome == VoyageCreateOutcome.SkippedConflict)
                throw new InvalidOperationException(result.Message ?? "Conflit de créneau véhicule.");
            if (result.Outcome == VoyageCreateOutcome.Failed)
                throw new ArgumentException(result.Message ?? "Échec création voyage.");
            return result.Voyage!;
        }

        public async Task<VoyageCreateResult> TryCreateAsync(
            Voyage voyage,
            IReadOnlyList<CreateVoyageEtapeDto>? etapesDestinations = null,
            VoyageCreateOptions? options = null)
        {
            return await CreateCoreAsync(voyage, etapesDestinations, options ?? new VoyageCreateOptions { ThrowOnConflict = false });
        }

        private async Task<VoyageCreateResult> CreateCoreAsync(
            Voyage voyage,
            IReadOnlyList<CreateVoyageEtapeDto>? etapesDestinations,
            VoyageCreateOptions options)
        {
            try
            {
                var etapesOrdered = ResolveEtapesOrdered(voyage.IdDestination, etapesDestinations);
                voyage.IdDestination = etapesOrdered[0].IdDestination;

                var vehiculeExists = await _context.Vehicules.AnyAsync(vh => vh.IdVehicule == voyage.IdVehicule);
                if (!vehiculeExists)
                    return VoyageCreateResult.Failed($"Le véhicule avec l'ID {voyage.IdVehicule} n'existe pas");

                var vehiculeSociete = await _context.Vehicules.AsNoTracking()
                    .Where(vh => vh.IdVehicule == voyage.IdVehicule)
                    .Select(vh => vh.IdSociete)
                    .FirstAsync();
                if (vehiculeSociete != voyage.IdSociete)
                    return VoyageCreateResult.Failed($"Le véhicule {voyage.IdVehicule} n'appartient pas à la société {voyage.IdSociete}.");

                if (!voyage.IdSite.HasValue)
                    return VoyageCreateResult.Failed("IdSite est obligatoire pour créer un voyage.");

                var site = await _context.Sites.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.IdSite == voyage.IdSite.Value);
                if (site == null)
                    return VoyageCreateResult.Failed($"Le site avec l'ID {voyage.IdSite.Value} n'existe pas.");
                if (site.IdSociete != voyage.IdSociete)
                    return VoyageCreateResult.Failed($"Le site {voyage.IdSite.Value} n'appartient pas à la société {voyage.IdSociete}.");

                var conversion = await ResolveVoyagePrixConversionAsync(voyage.IdSociete, voyage.CodeDevisePrix, voyage.DateDepart);
                voyage.CodeDevisePrix = conversion.CodeDevisePrix;
                voyage.CodeDevisePrincipale = conversion.CodeDevisePrincipale;
                voyage.TauxVersDevisePrincipale = conversion.Taux;
                voyage.PrixDevisePrincipale = Math.Round(voyage.Prix * conversion.Taux, 2, MidpointRounding.AwayFromZero);

                try
                {
                    await ValidateEtapesPourSocieteAsync(voyage.IdSociete, etapesOrdered);
                }
                catch (ArgumentException ex)
                {
                    return VoyageCreateResult.Failed(ex.Message);
                }

                var exists = await ExistsByVehiculeAndDateAsync(voyage.IdVehicule, voyage.DateDepart, voyage.HeureDepart);
                if (exists)
                {
                    var msg = $"Un voyage existe déjà pour le véhicule {voyage.IdVehicule} à la date {voyage.DateDepart:dd/MM/yyyy} et heure {voyage.HeureDepart:hh\\:mm}";
                    if (options.ThrowOnConflict)
                        throw new InvalidOperationException(msg);
                    return VoyageCreateResult.Skipped(msg);
                }

                if (options.IdPlanificationVoyage.HasValue)
                    voyage.IdPlanificationVoyage = options.IdPlanificationVoyage;

                voyage.DateCreation = DateTime.Now;
                _context.Voyages.Add(voyage);
                await _context.SaveChangesAsync();

                foreach (var step in etapesOrdered)
                {
                    _context.VoyageDestinations.Add(new VoyageDestination
                    {
                        IdVoyage = voyage.Id,
                        IdDestination = step.IdDestination,
                        Ordre = step.Ordre,
                        IdSociete = voyage.IdSociete,
                        DateCreation = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();

                await _voyageTarifService.EnsureDefaultEcoTarifAsync(voyage.Id, voyage.IdSociete, voyage.Prix);

                _logger.LogInformation(
                    "Voyage créé avec succès - ID: {VoyageId}, Vehicule: {IdVehicule}, Étapes: {NbEtapes}",
                    voyage.Id, voyage.IdVehicule, etapesOrdered.Count);

                return VoyageCreateResult.Created(voyage);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du voyage");
                if (options.ThrowOnConflict)
                    throw;
                return VoyageCreateResult.Failed(ex.Message);
            }
        }

        public async Task<Voyage?> UpdateAsync(Voyage voyage, IReadOnlyList<CreateVoyageEtapeDto>? etapesDestinations = null)
        {
            try
            {
                var existingVoyage = await _context.Voyages.FindAsync(voyage.Id);
                if (existingVoyage == null)
                    return null;

                var etapesOrdered = ResolveEtapesOrdered(voyage.IdDestination, etapesDestinations);
                voyage.IdDestination = etapesOrdered[0].IdDestination;

                var vehiculeExists = await _context.Vehicules.AnyAsync(vh => vh.IdVehicule == voyage.IdVehicule);
                if (!vehiculeExists)
                    throw new ArgumentException($"Le véhicule avec l'ID {voyage.IdVehicule} n'existe pas");

                var vehiculeSociete2 = await _context.Vehicules.AsNoTracking()
                    .Where(vh => vh.IdVehicule == voyage.IdVehicule)
                    .Select(vh => vh.IdSociete)
                    .FirstAsync();
                if (vehiculeSociete2 != voyage.IdSociete)
                    throw new ArgumentException($"Le véhicule {voyage.IdVehicule} n'appartient pas à la société {voyage.IdSociete}.");

                if (!voyage.IdSite.HasValue)
                    throw new ArgumentException("IdSite est obligatoire pour modifier un voyage.");

                var site = await _context.Sites.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.IdSite == voyage.IdSite.Value);
                if (site == null)
                    throw new ArgumentException($"Le site avec l'ID {voyage.IdSite.Value} n'existe pas.");
                if (site.IdSociete != voyage.IdSociete)
                    throw new ArgumentException($"Le site {voyage.IdSite.Value} n'appartient pas à la société {voyage.IdSociete}.");

                var conversion = await ResolveVoyagePrixConversionAsync(voyage.IdSociete, voyage.CodeDevisePrix, voyage.DateDepart);

                await ValidateEtapesPourSocieteAsync(voyage.IdSociete, etapesOrdered);

                var exists = await _context.Voyages
                    .AnyAsync(v => v.IdVehicule == voyage.IdVehicule &&
                                   v.DateDepart == voyage.DateDepart &&
                                   v.HeureDepart == voyage.HeureDepart &&
                                   v.Id != voyage.Id);

                if (exists)
                {
                    throw new InvalidOperationException(
                        $"Un voyage existe déjà pour le véhicule {voyage.IdVehicule} à la date {voyage.DateDepart:dd/MM/yyyy} et heure {voyage.HeureDepart:hh\\:mm}");
                }

                existingVoyage.DateDepart = voyage.DateDepart;
                existingVoyage.HeureDepart = voyage.HeureDepart;
                existingVoyage.Prix = voyage.Prix;
                existingVoyage.CodeDevisePrix = conversion.CodeDevisePrix;
                existingVoyage.CodeDevisePrincipale = conversion.CodeDevisePrincipale;
                existingVoyage.TauxVersDevisePrincipale = conversion.Taux;
                existingVoyage.PrixDevisePrincipale = Math.Round(voyage.Prix * conversion.Taux, 2, MidpointRounding.AwayFromZero);
                existingVoyage.IdVehicule = voyage.IdVehicule;
                existingVoyage.IdDestination = etapesOrdered[0].IdDestination;
                existingVoyage.IdSite = voyage.IdSite;
                existingVoyage.Statut = voyage.Statut;
                existingVoyage.DateModification = DateTime.Now;

                var anciennesEtapes = await _context.VoyageDestinations
                    .Where(vd => vd.IdVoyage == existingVoyage.Id)
                    .ToListAsync();
                _context.VoyageDestinations.RemoveRange(anciennesEtapes);

                foreach (var step in etapesOrdered)
                {
                    _context.VoyageDestinations.Add(new VoyageDestination
                    {
                        IdVoyage = existingVoyage.Id,
                        IdDestination = step.IdDestination,
                        Ordre = step.Ordre,
                        IdSociete = voyage.IdSociete,
                        DateCreation = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Voyage mis à jour avec succès - ID: {VoyageId}", voyage.Id);

                return existingVoyage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du voyage {VoyageId}", voyage.Id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task EnsurePrixUpdateAllowedAsync(
            int idVoyage,
            int nouveauPrix,
            bool tarifsFournis,
            CancellationToken cancellationToken = default)
        {
            if (tarifsFournis)
                return;

            var ancienPrix = await _context.Voyages.AsNoTracking()
                .Where(v => v.Id == idVoyage)
                .Select(v => v.Prix)
                .FirstOrDefaultAsync(cancellationToken);

            if (ancienPrix == nouveauPrix)
                return;

            if (await _voyageTarifService.HasTarifsForVoyageAsync(idVoyage, cancellationToken))
            {
                throw new ArgumentException(
                    "Pour modifier le prix, précisez la catégorie de siège via tarifs[], " +
                    "PUT /api/Voyage/{id}/tarifs-categorie-siege ou " +
                    "PATCH /api/Voyage/{id}/tarifs-categorie-siege/{idCategorieSiege}.");
            }
        }

        /// <inheritdoc />
        public async Task SyncVoyagePrixReferenceFromTarifsAsync(int idVoyage, CancellationToken cancellationToken = default)
        {
            var voyage = await _context.Voyages.FindAsync(new object[] { idVoyage }, cancellationToken);
            if (voyage == null)
                return;

            var referencePrix = await _voyageTarifService.ResolveReferencePrixFromTarifsAsync(
                idVoyage,
                voyage.IdSociete,
                voyage.Prix,
                cancellationToken);

            if (referencePrix == voyage.Prix)
                return;

            var conversion = await ResolveVoyagePrixConversionAsync(
                voyage.IdSociete,
                voyage.CodeDevisePrix,
                voyage.DateDepart);

            voyage.Prix = referencePrix;
            voyage.CodeDevisePrix = conversion.CodeDevisePrix;
            voyage.CodeDevisePrincipale = conversion.CodeDevisePrincipale;
            voyage.TauxVersDevisePrincipale = conversion.Taux;
            voyage.PrixDevisePrincipale = Math.Round(referencePrix * conversion.Taux, 2, MidpointRounding.AwayFromZero);
            voyage.DateModification = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);
        }

        private static List<CreateVoyageEtapeDto> ResolveEtapesOrdered(
            int idDestinationPrincipal,
            IReadOnlyList<CreateVoyageEtapeDto>? etapesDestinations)
        {
            if (etapesDestinations != null && etapesDestinations.Count > 0)
                return etapesDestinations.OrderBy(e => e.Ordre).ToList();

            return new List<CreateVoyageEtapeDto>
            {
                new CreateVoyageEtapeDto { Ordre = 1, IdDestination = idDestinationPrincipal }
            };
        }

        private async Task ValidateEtapesPourSocieteAsync(int idSocieteVoyage, IReadOnlyList<CreateVoyageEtapeDto> etapesOrdered)
        {
            foreach (var e in etapesOrdered)
            {
                var dest = await _context.Destinations.AsNoTracking()
                    .FirstOrDefaultAsync(d => d.IdDestination == e.IdDestination);
                if (dest == null)
                    throw new ArgumentException($"La destination avec l'ID {e.IdDestination} n'existe pas.");

                if (dest.IdSociete != idSocieteVoyage)
                {
                    throw new ArgumentException(
                        $"La destination {e.IdDestination} n'appartient pas à la société {idSocieteVoyage}.");
                }
            }
        }

        private async Task<(string CodeDevisePrix, string CodeDevisePrincipale, decimal Taux)> ResolveVoyagePrixConversionAsync(
            int idSociete,
            string codeDevisePrixInput,
            DateTime dateDepart)
        {
            var codeDevisePrix = string.IsNullOrWhiteSpace(codeDevisePrixInput)
                ? "CDF"
                : codeDevisePrixInput.Trim().ToUpperInvariant();

            var devisePrixExists = await _context.DevisesMonetaires.AsNoTracking()
                .AnyAsync(d => d.CodeDevise == codeDevisePrix && d.Statut);
            if (!devisePrixExists)
                throw new ArgumentException($"La devise prix '{codeDevisePrix}' n'est pas active.");

            var societe = await _context.Societes.AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdSociete == idSociete);
            if (societe == null)
                throw new ArgumentException($"Société {idSociete} introuvable.");

            var codeDevisePrincipale = string.IsNullOrWhiteSpace(societe.CodeDevisePrincipale)
                ? "CDF"
                : societe.CodeDevisePrincipale.Trim().ToUpperInvariant();

            if (codeDevisePrix == codeDevisePrincipale)
                return (codeDevisePrix, codeDevisePrincipale, 1m);

            var taux = await _context.TauxChanges.AsNoTracking()
                .Where(t => t.IdSociete == idSociete
                            && t.CodeDeviseSource == codeDevisePrix
                            && t.CodeDeviseCible == codeDevisePrincipale
                            && t.Statut
                            && t.DateEffet <= dateDepart)
                .OrderByDescending(t => t.DateEffet)
                .ThenByDescending(t => t.DateCreation)
                .Select(t => (decimal?)t.Taux)
                .FirstOrDefaultAsync();
            if (!taux.HasValue)
                throw new ArgumentException(
                    $"Aucun taux actif trouvé pour {codeDevisePrix}->{codeDevisePrincipale} à la date {dateDepart:yyyy-MM-dd}.");

            return (codeDevisePrix, codeDevisePrincipale, taux.Value);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var voyage = await _context.Voyages.FindAsync(id);
                if (voyage == null)
                    return false;

                _context.Voyages.Remove(voyage);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Voyage supprimé avec succès - ID: {VoyageId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du voyage {VoyageId}", id);
                throw;
            }
        }

        // Méthodes de recherche
        public async Task<IEnumerable<Voyage>> GetBySocieteAsync(int idSociete, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null)
        {
            try
            {
                var query = ApplyDateDepartRange(
                    _context.Voyages
                        .Include(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                        .Include(v => v.Vehicule)
                            .ThenInclude(vh => vh.Photos)
                        .Include(v => v.Destination)
                        .Include(v => v.VoyageTarifsCategorieSiege)
                            .ThenInclude(t => t.CategorieSiege)
                        .Where(v => v.IdSociete == idSociete),
                    dateDepartDebut,
                    dateDepartFin);

                return await query
                    .OrderBy(v => v.DateDepart)
                    .ThenBy(v => v.HeureDepart)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des voyages pour la société {SocieteId}", idSociete);
                throw;
            }
        }

        public async Task<IEnumerable<Voyage>> GetBySocietePublicAsync(int idSociete, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null)
        {
            try
            {
                var query = ApplyDateDepartRange(
                    BuildVoyageReadQuery(publicOnly: true)
                        .Where(v => v.IdSociete == idSociete),
                    dateDepartDebut,
                    dateDepartFin);

                return await query
                    .OrderBy(v => v.DateDepart)
                    .ThenBy(v => v.HeureDepart)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération publique des voyages pour la société {SocieteId}", idSociete);
                throw;
            }
        }

        public async Task<IEnumerable<Voyage>> GetBySiteAsync(int idSite, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null)
        {
            try
            {
                var query = ApplyDateDepartRange(
                    _context.Voyages
                        .Include(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                        .Include(v => v.Vehicule)
                            .ThenInclude(vh => vh.Photos)
                        .Include(v => v.Destination)
                        .Include(v => v.Site)
                        .Include(v => v.VoyageTarifsCategorieSiege)
                            .ThenInclude(t => t.CategorieSiege)
                        .Where(v => v.IdSite == idSite),
                    dateDepartDebut,
                    dateDepartFin);

                return await query
                    .OrderBy(v => v.DateDepart)
                    .ThenBy(v => v.HeureDepart)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des voyages pour le site {SiteId}", idSite);
                throw;
            }
        }

        public async Task<IEnumerable<Voyage>> GetBySitePublicAsync(int idSite, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null)
        {
            try
            {
                var query = ApplyDateDepartRange(
                    BuildVoyageReadQuery(publicOnly: true)
                        .Where(v => v.IdSite == idSite),
                    dateDepartDebut,
                    dateDepartFin);

                return await query
                    .OrderBy(v => v.DateDepart)
                    .ThenBy(v => v.HeureDepart)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération publique des voyages pour le site {SiteId}", idSite);
                throw;
            }
        }

        public async Task<IEnumerable<Voyage>> GetByVehiculeAsync(int idVehicule, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null)
        {
            try
            {
                var query = ApplyDateDepartRange(
                    _context.Voyages
                        .Include(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                        .Include(v => v.Vehicule)
                            .ThenInclude(vh => vh.Photos)
                        .Include(v => v.Destination)
                        .Include(v => v.Site)
                        .Include(v => v.VoyageTarifsCategorieSiege)
                            .ThenInclude(t => t.CategorieSiege)
                        .Where(v => v.IdVehicule == idVehicule),
                    dateDepartDebut,
                    dateDepartFin);

                return await query
                    .OrderBy(v => v.DateDepart)
                    .ThenBy(v => v.HeureDepart)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des voyages pour le véhicule {VehiculeId}", idVehicule);
                throw;
            }
        }

        public async Task<IEnumerable<Voyage>> GetByDestinationAsync(int idDestination, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null)
        {
            try
            {
                var query = ApplyDateDepartRange(
                    _context.Voyages
                        .Include(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                        .Include(v => v.Vehicule)
                            .ThenInclude(vh => vh.Photos)
                        .Include(v => v.Destination)
                        .Include(v => v.Site)
                        .Include(v => v.VoyageTarifsCategorieSiege)
                            .ThenInclude(t => t.CategorieSiege)
                        .Where(v => v.IdDestination == idDestination),
                    dateDepartDebut,
                    dateDepartFin);

                return await query
                    .OrderBy(v => v.DateDepart)
                    .ThenBy(v => v.HeureDepart)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des voyages pour la destination {DestinationId}", idDestination);
                throw;
            }
        }

        public async Task<IEnumerable<Voyage>> GetByDestinationPublicAsync(int idDestination, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null)
        {
            try
            {
                var query = ApplyDateDepartRange(
                    BuildVoyageReadQuery(publicOnly: true)
                        .Where(v => v.IdDestination == idDestination),
                    dateDepartDebut,
                    dateDepartFin);

                return await query
                    .OrderBy(v => v.DateDepart)
                    .ThenBy(v => v.HeureDepart)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération publique des voyages pour la destination {DestinationId}", idDestination);
                throw;
            }
        }

        public async Task<IEnumerable<Voyage>> GetByDateAsync(DateTime date)
        {
            try
            {
                return await _context.Voyages
                    .Include(v => v.Vehicule)
                        .ThenInclude(vh => vh.TypeVehicule)
                    .Include(v => v.Vehicule)
                        .ThenInclude(vh => vh.Photos)
                    .Include(v => v.Destination)
                    .Include(v => v.Site)
                    .Include(v => v.VoyageTarifsCategorieSiege)
                        .ThenInclude(t => t.CategorieSiege)
                    .Where(v => v.DateDepart.Date == date.Date)
                    .OrderBy(v => v.HeureDepart)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des voyages pour la date {Date}", date);
                throw;
            }
        }

        public async Task<IEnumerable<Voyage>> GetByDatePublicAsync(DateTime date)
        {
            try
            {
                return await BuildVoyageReadQuery(publicOnly: true)
                    .Where(v => v.DateDepart.Date == date.Date)
                    .OrderBy(v => v.HeureDepart)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération publique des voyages pour la date {Date}", date);
                throw;
            }
        }

        public async Task<IEnumerable<Voyage>> GetByVehiculeAndDestinationAsync(int idVehicule, int idDestination)
        {
            try
            {
                return await _context.Voyages
                    .Include(v => v.Vehicule)
                        .ThenInclude(vh => vh.TypeVehicule)
                    .Include(v => v.Vehicule)
                        .ThenInclude(vh => vh.Photos)
                    .Include(v => v.Destination)
                    .Include(v => v.Site)
                    .Include(v => v.VoyageTarifsCategorieSiege)
                        .ThenInclude(t => t.CategorieSiege)
                    .Where(v => v.IdVehicule == idVehicule && v.IdDestination == idDestination)
                    .OrderBy(v => v.DateDepart)
                    .ThenBy(v => v.HeureDepart)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des voyages pour le véhicule {VehiculeId} et destination {DestinationId}", idVehicule, idDestination);
                throw;
            }
        }

        public async Task<IEnumerable<Voyage>> GetByVehiculeAndDestinationPublicAsync(int idVehicule, int idDestination)
        {
            try
            {
                return await BuildVoyageReadQuery(publicOnly: true)
                    .Where(v => v.IdVehicule == idVehicule && v.IdDestination == idDestination)
                    .OrderBy(v => v.DateDepart)
                    .ThenBy(v => v.HeureDepart)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération publique des voyages pour le véhicule {VehiculeId} et destination {DestinationId}", idVehicule, idDestination);
                throw;
            }
        }

        public async Task<IEnumerable<Voyage>> GetByDateRangeAsync(DateTime dateDebut, DateTime dateFin)
        {
            try
            {
                return await _context.Voyages
                    .Include(v => v.Vehicule)
                        .ThenInclude(vh => vh.TypeVehicule)
                    .Include(v => v.Vehicule)
                        .ThenInclude(vh => vh.Photos)
                    .Include(v => v.Destination)
                    .Include(v => v.Site)
                    .Include(v => v.VoyageTarifsCategorieSiege)
                        .ThenInclude(t => t.CategorieSiege)
                    .Where(v => v.DateDepart.Date >= dateDebut.Date && v.DateDepart.Date <= dateFin.Date)
                    .OrderBy(v => v.DateDepart)
                    .ThenBy(v => v.HeureDepart)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des voyages entre {DateDebut} et {DateFin}", dateDebut, dateFin);
                throw;
            }
        }

        public async Task<IEnumerable<Voyage>> GetByDateRangePublicAsync(DateTime dateDebut, DateTime dateFin)
        {
            try
            {
                return await BuildVoyageReadQuery(publicOnly: true)
                    .Where(v => v.DateDepart.Date >= dateDebut.Date && v.DateDepart.Date <= dateFin.Date)
                    .OrderBy(v => v.DateDepart)
                    .ThenBy(v => v.HeureDepart)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération publique des voyages entre {DateDebut} et {DateFin}", dateDebut, dateFin);
                throw;
            }
        }

        // Méthodes de filtrage
        public async Task<IEnumerable<Voyage>> GetByStatutAsync(bool statut)
        {
            try
            {
                return await _context.Voyages
                    .Include(v => v.Vehicule)
                        .ThenInclude(vh => vh.TypeVehicule)
                    .Include(v => v.Vehicule)
                        .ThenInclude(vh => vh.Photos)
                    .Include(v => v.Destination)
                    .Include(v => v.Site)
                    .Include(v => v.VoyageTarifsCategorieSiege)
                        .ThenInclude(t => t.CategorieSiege)
                    .Where(v => v.Statut == statut)
                    .OrderByDescending(v => v.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des voyages avec statut {Statut}", statut);
                throw;
            }
        }

        public async Task<IEnumerable<Voyage>> GetByPriceRangeAsync(int prixMin, int prixMax)
        {
            try
            {
                return await _context.Voyages
                    .Include(v => v.Vehicule)
                        .ThenInclude(vh => vh.TypeVehicule)
                    .Include(v => v.Vehicule)
                        .ThenInclude(vh => vh.Photos)
                    .Include(v => v.Destination)
                    .Include(v => v.Site)
                    .Include(v => v.VoyageTarifsCategorieSiege)
                        .ThenInclude(t => t.CategorieSiege)
                    .Where(v => v.Prix >= prixMin && v.Prix <= prixMax)
                    .OrderBy(v => v.Prix)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des voyages avec prix entre {PrixMin} et {PrixMax}", prixMin, prixMax);
                throw;
            }
        }

        public async Task<IEnumerable<Voyage>> GetByPriceRangePublicAsync(int prixMin, int prixMax)
        {
            try
            {
                return await BuildVoyageReadQuery(publicOnly: true)
                    .Where(v => v.Prix >= prixMin && v.Prix <= prixMax)
                    .OrderBy(v => v.Prix)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération publique des voyages avec prix entre {PrixMin} et {PrixMax}", prixMin, prixMax);
                throw;
            }
        }

        // Méthodes d'existence
        public async Task<bool> ExistsAsync(int id)
        {
            try
            {
                return await _context.Voyages.AnyAsync(v => v.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification d'existence du voyage {VoyageId}", id);
                throw;
            }
        }

        public async Task<bool> ExistsPublicAsync(int id)
        {
            try
            {
                return await _context.Voyages.AnyAsync(v =>
                    v.Id == id &&
                    v.Societe != null &&
                    v.Societe.Statut == true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification publique d'existence du voyage {VoyageId}", id);
                throw;
            }
        }

        public async Task<bool> ExistsByVehiculeAndDateAsync(int idVehicule, DateTime date, TimeSpan heure)
        {
            try
            {
                return await _context.Voyages.AnyAsync(v => v.IdVehicule == idVehicule && 
                                                         v.DateDepart.Date == date.Date && 
                                                         v.HeureDepart == heure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification d'existence du voyage pour le véhicule {VehiculeId} à la date {Date} et heure {Heure}", idVehicule, date, heure);
                throw;
            }
        }

        // Pagination
        public async Task<PagedResult<Voyage>> GetPagedAsync(
            PagedRequest request,
            DateTime? dateDepartDebut = null,
            DateTime? dateDepartFin = null)
        {
            try
            {
                var query = ApplyDateDepartRange(
                    IncludeVehiculeNavigations(_context.Voyages)
                        .Include(v => v.Destination)
                        .Include(v => v.Site)
                        .Include(v => v.VoyageTarifsCategorieSiege)
                            .ThenInclude(t => t.CategorieSiege),
                    dateDepartDebut,
                    dateDepartFin);

                query = query.AsQueryable();

                query = ApplyVoyageSearchTerm(query, request.SearchTerm);

                var totalCount = await query.CountAsync();

                // Tri
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "date":
                            query = request.SortDescending 
                                ? query.OrderByDescending(v => v.DateDepart).ThenByDescending(v => v.HeureDepart)
                                : query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart);
                            break;
                        case "prix":
                            query = request.SortDescending 
                                ? query.OrderByDescending(v => v.Prix)
                                : query.OrderBy(v => v.Prix);
                            break;
                        case "vehicule":
                            query = request.SortDescending 
                                ? query.OrderByDescending(v => v.Vehicule != null ? v.Vehicule.AliasVehicule : "")
                                : query.OrderBy(v => v.Vehicule != null ? v.Vehicule.AliasVehicule : "");
                            break;
                        default:
                            query = query.OrderByDescending(v => v.DateCreation);
                            break;
                    }
                }
                else
                {
                    query = dateDepartDebut.HasValue && dateDepartFin.HasValue
                        ? query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart)
                        : query.OrderByDescending(v => v.DateCreation);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Voyage>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des voyages");
                throw;
            }
        }

        public async Task<PagedResult<Voyage>> GetPagedPublicAsync(
            PagedRequest request,
            DateTime? dateDepartDebut = null,
            DateTime? dateDepartFin = null)
        {
            try
            {
                var query = ApplyDateDepartRange(
                    BuildVoyageReadQuery(publicOnly: true),
                    dateDepartDebut,
                    dateDepartFin);

                query = query.AsQueryable();
                query = ApplyVoyageSearchTerm(query, request.SearchTerm);

                var totalCount = await query.CountAsync();

                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "date":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.DateDepart).ThenByDescending(v => v.HeureDepart)
                                : query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart);
                            break;
                        case "prix":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.Prix)
                                : query.OrderBy(v => v.Prix);
                            break;
                        case "vehicule":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.Vehicule != null ? v.Vehicule.AliasVehicule : "")
                                : query.OrderBy(v => v.Vehicule != null ? v.Vehicule.AliasVehicule : "");
                            break;
                        default:
                            query = query.OrderByDescending(v => v.DateCreation);
                            break;
                    }
                }
                else
                {
                    query = dateDepartDebut.HasValue && dateDepartFin.HasValue
                        ? query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart)
                        : query.OrderByDescending(v => v.DateCreation);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Voyage>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée publique des voyages");
                throw;
            }
        }

        public async Task<PagedResult<Voyage>> SearchPagedAsync(
            PagedRequest request,
            string? villeDepart = null,
            string? villeArrivee = null,
            int? idSociete = null,
            DateTime? dateDepartDebut = null,
            DateTime? dateDepartFin = null)
        {
            try
            {
                var depart = NormalizeSearchText(villeDepart);
                var arrivee = NormalizeSearchText(villeArrivee);

                var query = ApplyDateDepartRange(
                    IncludeVehiculeNavigations(_context.Voyages)
                        .Include(v => v.Destination)
                        .Include(v => v.Site)
                        .Include(v => v.VoyageTarifsCategorieSiege)
                            .ThenInclude(t => t.CategorieSiege),
                    dateDepartDebut,
                    dateDepartFin);

                if (idSociete.HasValue)
                    query = query.Where(v => v.IdSociete == idSociete.Value);

                if (depart != null)
                    query = query.Where(v =>
                        v.Destination != null &&
                        v.Destination.VilleDepart.ToLower().Contains(depart));

                if (arrivee != null)
                    query = query.Where(v =>
                        v.Destination != null &&
                        v.Destination.VilleArrivee.ToLower().Contains(arrivee));

                query = query.AsQueryable();
                query = ApplyVoyageSearchTerm(query, request.SearchTerm);

                var totalCount = await query.CountAsync();

                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "date":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.DateDepart).ThenByDescending(v => v.HeureDepart)
                                : query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart);
                            break;
                        case "prix":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.Prix)
                                : query.OrderBy(v => v.Prix);
                            break;
                        case "vehicule":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.Vehicule != null ? v.Vehicule.AliasVehicule : "")
                                : query.OrderBy(v => v.Vehicule != null ? v.Vehicule.AliasVehicule : "");
                            break;
                        default:
                            query = query.OrderByDescending(v => v.DateCreation);
                            break;
                    }
                }
                else
                {
                    query = OrderVoyagesForListe(query, dateDepartDebut.HasValue && dateDepartFin.HasValue);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Voyage>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erreur lors de la recherche paginée des voyages (idSociete: {IdSociete}, villeDepart: {VilleDepart}, villeArrivee: {VilleArrivee})",
                    idSociete,
                    villeDepart,
                    villeArrivee);
                throw;
            }
        }

        public async Task<PagedResult<Voyage>> SearchPagedPublicAsync(
            PagedRequest request,
            string? villeDepart = null,
            string? villeArrivee = null,
            int? idSociete = null,
            DateTime? dateDepartDebut = null,
            DateTime? dateDepartFin = null)
        {
            try
            {
                var depart = NormalizeSearchText(villeDepart);
                var arrivee = NormalizeSearchText(villeArrivee);

                var query = ApplyDateDepartRange(
                    BuildVoyageReadQuery(publicOnly: true),
                    dateDepartDebut,
                    dateDepartFin);

                if (idSociete.HasValue)
                    query = query.Where(v => v.IdSociete == idSociete.Value);

                if (depart != null)
                    query = query.Where(v =>
                        v.Destination != null &&
                        v.Destination.VilleDepart.ToLower().Contains(depart));

                if (arrivee != null)
                    query = query.Where(v =>
                        v.Destination != null &&
                        v.Destination.VilleArrivee.ToLower().Contains(arrivee));

                query = query.AsQueryable();
                query = ApplyVoyageSearchTerm(query, request.SearchTerm);

                var totalCount = await query.CountAsync();

                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "date":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.DateDepart).ThenByDescending(v => v.HeureDepart)
                                : query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart);
                            break;
                        case "prix":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.Prix)
                                : query.OrderBy(v => v.Prix);
                            break;
                        case "vehicule":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.Vehicule != null ? v.Vehicule.AliasVehicule : "")
                                : query.OrderBy(v => v.Vehicule != null ? v.Vehicule.AliasVehicule : "");
                            break;
                        default:
                            query = query.OrderByDescending(v => v.DateCreation);
                            break;
                    }
                }
                else
                {
                    query = OrderVoyagesForListe(query, dateDepartDebut.HasValue && dateDepartFin.HasValue);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Voyage>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erreur lors de la recherche paginée publique des voyages (idSociete: {IdSociete}, villeDepart: {VilleDepart}, villeArrivee: {VilleArrivee})",
                    idSociete,
                    villeDepart,
                    villeArrivee);
                throw;
            }
        }

        public async Task<PagedResult<Voyage>> GetBySocietePagedAsync(
            int idSociete,
            PagedRequest request,
            DateTime? dateDepartDebut = null,
            DateTime? dateDepartFin = null)
        {
            try
            {
                var query = ApplyDateDepartRange(
                    _context.Voyages
                        .Include(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                        .Include(v => v.Vehicule)
                            .ThenInclude(vh => vh.Photos)
                        .Include(v => v.Destination)
                        .Include(v => v.Site)
                        .Include(v => v.VoyageTarifsCategorieSiege)
                            .ThenInclude(t => t.CategorieSiege)
                        .Where(v => v.IdSociete == idSociete),
                    dateDepartDebut,
                    dateDepartFin);

                query = query.AsQueryable();

                query = ApplyVoyageSearchTerm(query, request.SearchTerm);

                var totalCount = await query.CountAsync();

                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "date":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.DateDepart).ThenByDescending(v => v.HeureDepart)
                                : query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart);
                            break;
                        case "prix":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.Prix)
                                : query.OrderBy(v => v.Prix);
                            break;
                        default:
                            query = query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart);
                            break;
                    }
                }
                else
                {
                    query = OrderVoyagesForListe(query, dateDepartDebut.HasValue && dateDepartFin.HasValue);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Voyage>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des voyages pour la société {SocieteId}", idSociete);
                throw;
            }
        }

        public async Task<PagedResult<Voyage>> GetBySocietePagedPublicAsync(
            int idSociete,
            PagedRequest request,
            DateTime? dateDepartDebut = null,
            DateTime? dateDepartFin = null)
        {
            try
            {
                var query = ApplyDateDepartRange(
                    BuildVoyageReadQuery(publicOnly: true)
                        .Where(v => v.IdSociete == idSociete),
                    dateDepartDebut,
                    dateDepartFin);

                query = query.AsQueryable();
                query = ApplyVoyageSearchTerm(query, request.SearchTerm);

                var totalCount = await query.CountAsync();

                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "date":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.DateDepart).ThenByDescending(v => v.HeureDepart)
                                : query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart);
                            break;
                        case "prix":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.Prix)
                                : query.OrderBy(v => v.Prix);
                            break;
                        default:
                            query = query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart);
                            break;
                    }
                }
                else
                {
                    query = OrderVoyagesForListe(query, dateDepartDebut.HasValue && dateDepartFin.HasValue);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Voyage>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée publique des voyages pour la société {SocieteId}", idSociete);
                throw;
            }
        }

        public async Task<PagedResult<Voyage>> GetBySitePagedAsync(
            int idSite,
            PagedRequest request,
            DateTime? dateDepartDebut = null,
            DateTime? dateDepartFin = null)
        {
            try
            {
                var query = ApplyDateDepartRange(
                    _context.Voyages
                        .Include(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                        .Include(v => v.Vehicule)
                            .ThenInclude(vh => vh.Photos)
                        .Include(v => v.Destination)
                        .Include(v => v.Site)
                        .Include(v => v.VoyageTarifsCategorieSiege)
                            .ThenInclude(t => t.CategorieSiege)
                        .Where(v => v.IdSite == idSite),
                    dateDepartDebut,
                    dateDepartFin);

                query = query.AsQueryable();

                query = ApplyVoyageSearchTerm(query, request.SearchTerm);

                var totalCount = await query.CountAsync();

                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "date":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.DateDepart).ThenByDescending(v => v.HeureDepart)
                                : query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart);
                            break;
                        case "prix":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.Prix)
                                : query.OrderBy(v => v.Prix);
                            break;
                        default:
                            query = query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart);
                            break;
                    }
                }
                else
                {
                    query = OrderVoyagesForListe(query, dateDepartDebut.HasValue && dateDepartFin.HasValue);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Voyage>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des voyages pour le site {SiteId}", idSite);
                throw;
            }
        }

        public async Task<PagedResult<Voyage>> GetBySitePagedPublicAsync(
            int idSite,
            PagedRequest request,
            DateTime? dateDepartDebut = null,
            DateTime? dateDepartFin = null)
        {
            try
            {
                var query = ApplyDateDepartRange(
                    BuildVoyageReadQuery(publicOnly: true)
                        .Where(v => v.IdSite == idSite),
                    dateDepartDebut,
                    dateDepartFin);

                query = query.AsQueryable();
                query = ApplyVoyageSearchTerm(query, request.SearchTerm);

                var totalCount = await query.CountAsync();

                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "date":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.DateDepart).ThenByDescending(v => v.HeureDepart)
                                : query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart);
                            break;
                        case "prix":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.Prix)
                                : query.OrderBy(v => v.Prix);
                            break;
                        default:
                            query = query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart);
                            break;
                    }
                }
                else
                {
                    query = OrderVoyagesForListe(query, dateDepartDebut.HasValue && dateDepartFin.HasValue);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Voyage>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée publique des voyages pour le site {SiteId}", idSite);
                throw;
            }
        }

        public async Task<PagedResult<Voyage>> GetByVehiculePagedAsync(
            int idVehicule,
            PagedRequest request,
            DateTime? dateDepartDebut = null,
            DateTime? dateDepartFin = null)
        {
            try
            {
                var query = ApplyDateDepartRange(
                    _context.Voyages
                        .Include(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                        .Include(v => v.Vehicule)
                            .ThenInclude(vh => vh.Photos)
                        .Include(v => v.Destination)
                        .Include(v => v.VoyageTarifsCategorieSiege)
                            .ThenInclude(t => t.CategorieSiege)
                        .Where(v => v.IdVehicule == idVehicule),
                    dateDepartDebut,
                    dateDepartFin);

                query = query.AsQueryable();

                query = ApplyVoyageSearchTerm(query, request.SearchTerm);

                var totalCount = await query.CountAsync();

                // Tri
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "date":
                            query = request.SortDescending 
                                ? query.OrderByDescending(v => v.DateDepart).ThenByDescending(v => v.HeureDepart)
                                : query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart);
                            break;
                        case "prix":
                            query = request.SortDescending 
                                ? query.OrderByDescending(v => v.Prix)
                                : query.OrderBy(v => v.Prix);
                            break;
                        default:
                            query = query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart);
                            break;
                    }
                }
                else
                {
                    query = OrderVoyagesForListe(query, dateDepartDebut.HasValue && dateDepartFin.HasValue);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Voyage>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des voyages pour le véhicule {VehiculeId}", idVehicule);
                throw;
            }
        }

        public async Task<PagedResult<Voyage>> GetByVehiculePagedPublicAsync(
            int idVehicule,
            PagedRequest request,
            DateTime? dateDepartDebut = null,
            DateTime? dateDepartFin = null)
        {
            try
            {
                var query = ApplyDateDepartRange(
                    BuildVoyageReadQuery(publicOnly: true)
                        .Where(v => v.IdVehicule == idVehicule),
                    dateDepartDebut,
                    dateDepartFin);

                query = query.AsQueryable();
                query = ApplyVoyageSearchTerm(query, request.SearchTerm);

                var totalCount = await query.CountAsync();

                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "date":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.DateDepart).ThenByDescending(v => v.HeureDepart)
                                : query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart);
                            break;
                        case "prix":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.Prix)
                                : query.OrderBy(v => v.Prix);
                            break;
                        default:
                            query = query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart);
                            break;
                    }
                }
                else
                {
                    query = OrderVoyagesForListe(query, dateDepartDebut.HasValue && dateDepartFin.HasValue);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Voyage>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée publique des voyages pour le véhicule {VehiculeId}", idVehicule);
                throw;
            }
        }

        public async Task<PagedResult<Voyage>> GetByDestinationPagedAsync(
            int idDestination,
            PagedRequest request,
            DateTime? dateDepartDebut = null,
            DateTime? dateDepartFin = null)
        {
            try
            {
                var query = ApplyDateDepartRange(
                    _context.Voyages
                        .Include(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                        .Include(v => v.Vehicule)
                            .ThenInclude(vh => vh.Photos)
                        .Include(v => v.Destination)
                        .Include(v => v.VoyageTarifsCategorieSiege)
                            .ThenInclude(t => t.CategorieSiege)
                        .Where(v => v.IdDestination == idDestination),
                    dateDepartDebut,
                    dateDepartFin);

                query = query.AsQueryable();

                query = ApplyVoyageSearchTerm(query, request.SearchTerm);

                var totalCount = await query.CountAsync();

                // Tri
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "date":
                            query = request.SortDescending 
                                ? query.OrderByDescending(v => v.DateDepart).ThenByDescending(v => v.HeureDepart)
                                : query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart);
                            break;
                        case "prix":
                            query = request.SortDescending 
                                ? query.OrderByDescending(v => v.Prix)
                                : query.OrderBy(v => v.Prix);
                            break;
                        default:
                            query = query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart);
                            break;
                    }
                }
                else
                {
                    query = OrderVoyagesForListe(query, dateDepartDebut.HasValue && dateDepartFin.HasValue);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Voyage>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des voyages pour la destination {DestinationId}", idDestination);
                throw;
            }
        }

        public async Task<PagedResult<Voyage>> GetByDestinationPagedPublicAsync(
            int idDestination,
            PagedRequest request,
            DateTime? dateDepartDebut = null,
            DateTime? dateDepartFin = null)
        {
            try
            {
                var query = ApplyDateDepartRange(
                    BuildVoyageReadQuery(publicOnly: true)
                        .Where(v => v.IdDestination == idDestination),
                    dateDepartDebut,
                    dateDepartFin);

                query = query.AsQueryable();
                query = ApplyVoyageSearchTerm(query, request.SearchTerm);

                var totalCount = await query.CountAsync();

                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "date":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.DateDepart).ThenByDescending(v => v.HeureDepart)
                                : query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart);
                            break;
                        case "prix":
                            query = request.SortDescending
                                ? query.OrderByDescending(v => v.Prix)
                                : query.OrderBy(v => v.Prix);
                            break;
                        default:
                            query = query.OrderBy(v => v.DateDepart).ThenBy(v => v.HeureDepart);
                            break;
                    }
                }
                else
                {
                    query = OrderVoyagesForListe(query, dateDepartDebut.HasValue && dateDepartFin.HasValue);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Voyage>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée publique des voyages pour la destination {DestinationId}", idDestination);
                throw;
            }
        }

        // Compteurs
        public async Task<int> CountAsync()
        {
            try
            {
                return await _context.Voyages.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des voyages");
                throw;
            }
        }

        public async Task<int> CountByVehiculeAsync(int idVehicule)
        {
            try
            {
                return await _context.Voyages.CountAsync(v => v.IdVehicule == idVehicule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des voyages pour le véhicule {VehiculeId}", idVehicule);
                throw;
            }
        }

        public async Task<int> CountByDestinationAsync(int idDestination)
        {
            try
            {
                return await _context.Voyages.CountAsync(v => v.IdDestination == idDestination);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des voyages pour la destination {DestinationId}", idDestination);
                throw;
            }
        }

        public async Task<int> CountByDateAsync(DateTime date)
        {
            try
            {
                return await _context.Voyages.CountAsync(v => v.DateDepart.Date == date.Date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des voyages pour la date {Date}", date);
                throw;
            }
        }

        public async Task<int> CountByStatutAsync(bool statut)
        {
            try
            {
                return await _context.Voyages.CountAsync(v => v.Statut == statut);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des voyages avec statut {Statut}", statut);
                throw;
            }
        }

        public async Task<IReadOnlyList<VoyageDestination>> GetOrderedDestinationsAsync(int idVoyage)
        {
            try
            {
                return await _context.VoyageDestinations
                    .Include(vd => vd.Destination)
                    .Where(vd => vd.IdVoyage == idVoyage)
                    .OrderBy(vd => vd.Ordre)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la lecture des destinations du voyage {VoyageId}", idVoyage);
                throw;
            }
        }

        public async Task<IReadOnlyList<VoyageDestination>> GetOrderedDestinationsPublicAsync(int idVoyage)
        {
            try
            {
                return await _context.VoyageDestinations
                    .Include(vd => vd.Destination)
                    .Where(vd =>
                        vd.IdVoyage == idVoyage &&
                        vd.Voyage != null &&
                        vd.Voyage.Societe != null &&
                        vd.Voyage.Societe.Statut == true)
                    .OrderBy(vd => vd.Ordre)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la lecture publique des destinations du voyage {VoyageId}", idVoyage);
                throw;
            }
        }

        public async Task<IReadOnlyList<Siege>> GetSiegesDisponiblesPourVoyageAsync(int idVoyage)
        {
            try
            {
                var voyage = await _context.Voyages
                    .AsNoTracking()
                    .Include(v => v.Vehicule)
                    .FirstOrDefaultAsync(v => v.Id == idVoyage);

                if (voyage?.Vehicule == null)
                    return Array.Empty<Siege>();

                var indisponibles = await _siegeDisponibilite.GetIndisponibleSiegeIdsAsync(idVoyage);

                return await _context.Sieges
                    .AsNoTracking()
                    .Where(s => s.IdVehicule == voyage.IdVehicule
                                && s.EstActif
                                && s.NumeroOrdre <= voyage.Vehicule.NombreSiege
                                && !indisponibles.Contains(s.IdSiege))
                    .OrderBy(s => s.NumeroOrdre)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul des sièges disponibles pour le voyage {VoyageId}", idVoyage);
                throw;
            }
        }

        public async Task<VoyageSiegesDisponiblesResponseDto> GetSiegesDisponiblesResponsePourVoyageAsync(int idVoyage)
        {
            try
            {
                var voyage = await _context.Voyages
                    .AsNoTracking()
                    .Include(v => v.Vehicule)
                    .FirstOrDefaultAsync(v => v.Id == idVoyage);

                if (voyage?.Vehicule == null)
                {
                    return new VoyageSiegesDisponiblesResponseDto
                    {
                        IdVoyage = idVoyage,
                        NombreSiegesDisponibles = 0,
                        RepartitionCategorieSieges = new List<VoyageCategorieSiegeDisponiblesDto>()
                    };
                }

                var indisponibles = await _siegeDisponibilite.GetIndisponibleSiegeIdsAsync(idVoyage);

                var sieges = await _context.Sieges
                    .AsNoTracking()
                    .Include(s => s.CategorieSiege)
                    .Where(s => s.IdVehicule == voyage.IdVehicule
                                && s.EstActif
                                && s.NumeroOrdre <= voyage.Vehicule.NombreSiege
                                && !indisponibles.Contains(s.IdSiege))
                    .OrderBy(s => s.NumeroOrdre)
                    .ToListAsync();

                var repartition = BuildRepartitionCategorieSiegesDisponibles(sieges);

                return new VoyageSiegesDisponiblesResponseDto
                {
                    IdVoyage = idVoyage,
                    NombreSiegesDisponibles = sieges.Count,
                    RepartitionCategorieSieges = repartition
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul des sièges disponibles groupés pour le voyage {VoyageId}", idVoyage);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<int, List<VoyageCategorieSiegeDisponiblesSummaryDto>>>
            GetRepartitionSiegesDisponiblesParVoyagesAsync(IReadOnlyList<int> idVoyages)
        {
            if (idVoyages.Count == 0)
                return new Dictionary<int, List<VoyageCategorieSiegeDisponiblesSummaryDto>>();

            try
            {
                var idList = idVoyages.Distinct().ToList();

                var voyages = await _context.Voyages
                    .AsNoTracking()
                    .Include(v => v.Vehicule)
                    .Where(v => idList.Contains(v.Id))
                    .Select(v => new
                    {
                        v.Id,
                        v.IdVehicule,
                        NombreSiege = v.Vehicule != null ? v.Vehicule.NombreSiege : 0
                    })
                    .ToListAsync();

                if (voyages.Count == 0)
                    return new Dictionary<int, List<VoyageCategorieSiegeDisponiblesSummaryDto>>();

                var vehiculeIds = voyages.Select(v => v.IdVehicule).Distinct().ToList();

                var takenByVoyage = await _siegeDisponibilite.GetIndisponibleSiegeIdsParVoyagesAsync(idList);

                var siegesParVehicule = await _context.Sieges
                    .AsNoTracking()
                    .Include(s => s.CategorieSiege)
                    .Where(s => vehiculeIds.Contains(s.IdVehicule) && s.EstActif)
                    .ToListAsync();

                var siegesByVehiculeId = siegesParVehicule
                    .GroupBy(s => s.IdVehicule)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var result = new Dictionary<int, List<VoyageCategorieSiegeDisponiblesSummaryDto>>();

                foreach (var voy in voyages)
                {
                    if (voy.NombreSiege <= 0
                        || !siegesByVehiculeId.TryGetValue(voy.IdVehicule, out var siegesVehicule))
                    {
                        result[voy.Id] = new List<VoyageCategorieSiegeDisponiblesSummaryDto>();
                        continue;
                    }

                    var taken = takenByVoyage.GetValueOrDefault(voy.Id) ?? new HashSet<int>();
                    var disponibles = siegesVehicule
                        .Where(s => s.NumeroOrdre <= voy.NombreSiege && !taken.Contains(s.IdSiege))
                        .ToList();

                    result[voy.Id] = BuildRepartitionCategorieSiegesDisponiblesSummary(disponibles);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul groupé des sièges disponibles par catégorie");
                throw;
            }
        }

        private static List<VoyageCategorieSiegeDisponiblesDto> BuildRepartitionCategorieSiegesDisponibles(
            IReadOnlyList<Siege> sieges) =>
            sieges
                .GroupBy(s => s.IdCategorieSiege)
                .Select(g =>
                {
                    var categorie = g.First().CategorieSiege;
                    return new VoyageCategorieSiegeDisponiblesDto
                    {
                        IdCategorieSiege = g.Key,
                        CodeCategorieSiege = categorie?.CodeCategorieSiege ?? string.Empty,
                        Libelle = categorie?.Libelle ?? string.Empty,
                        NombreSiege = g.Count(),
                        Sieges = g.Select(s => new SiegeLibreReadDto
                        {
                            IdSiege = s.IdSiege,
                            NumeroOrdre = s.NumeroOrdre,
                            CodeSiege = s.CodeSiege
                        }).ToList()
                    };
                })
                .OrderBy(r => r.CodeCategorieSiege, StringComparer.OrdinalIgnoreCase)
                .ToList();

        private static List<VoyageCategorieSiegeDisponiblesSummaryDto> BuildRepartitionCategorieSiegesDisponiblesSummary(
            IReadOnlyList<Siege> sieges) =>
            sieges
                .GroupBy(s => s.IdCategorieSiege)
                .Select(g =>
                {
                    var categorie = g.First().CategorieSiege;
                    return new VoyageCategorieSiegeDisponiblesSummaryDto
                    {
                        IdCategorieSiege = g.Key,
                        CodeCategorieSiege = categorie?.CodeCategorieSiege ?? string.Empty,
                        Libelle = categorie?.Libelle ?? string.Empty,
                        NombreSiege = g.Count()
                    };
                })
                .OrderBy(r => r.CodeCategorieSiege, StringComparer.OrdinalIgnoreCase)
                .ToList();

        public async Task<IReadOnlyList<VoyageSeatAllocation>> GetAllocationsConfirmePourVoyageAsync(int idVoyage)
        {
            try
            {
                var voyage = await _context.Voyages
                    .AsNoTracking()
                    .Include(v => v.Vehicule)
                    .FirstOrDefaultAsync(v => v.Id == idVoyage);

                if (voyage?.Vehicule == null)
                    return Array.Empty<VoyageSeatAllocation>();

                return await _context.VoyageSeatAllocations
                    .AsNoTracking()
                    .Include(a => a.Siege)
                    .Include(a => a.ReservationPassenger)
                    .Where(a => a.IdVoyage == idVoyage
                                && a.Statut == "CONFIRME"
                                && a.Siege != null
                                && a.Siege.IdVehicule == voyage.IdVehicule
                                && a.Siege.EstActif
                                && a.Siege.NumeroOrdre <= voyage.Vehicule.NombreSiege)
                    .OrderBy(a => a.Siege!.NumeroOrdre)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul des sièges indisponibles pour le voyage {VoyageId}", idVoyage);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<PassagersEmbarquesQueryResult> GetPassagersEmbarquesPourCriteresVoyageAsync(
            int idDestination,
            int idVehicule,
            DateTime dateDepart,
            TimeSpan? heureDepart = null)
        {
            try
            {
                var jour = dateDepart.Date;

                var query = _context.Voyages.AsNoTracking()
                    .Where(v => v.IdDestination == idDestination
                                && v.IdVehicule == idVehicule
                                && v.DateDepart.Date == jour);

                if (heureDepart.HasValue)
                    query = query.Where(v => v.HeureDepart == heureDepart.Value);

                var voyageIds = await query.Select(v => v.Id).ToListAsync();

                if (voyageIds.Count == 0)
                {
                    var suffixeHeure = heureDepart.HasValue
                        ? $" et à l'heure de départ {heureDepart.Value:hh\\:mm}"
                        : "";
                    return PassagersEmbarquesQueryResult.NoVoyage(
                        $"Aucun voyage ne correspond à la destination {idDestination}, au véhicule {idVehicule} et à la date du {jour:dd/MM/yyyy}{suffixeHeure}.");
                }

                if (voyageIds.Count > 1)
                {
                    var detailHeure = heureDepart.HasValue
                        ? $" à l'heure {heureDepart.Value:hh\\:mm}"
                        : " (heures de départ différentes)";
                    return PassagersEmbarquesQueryResult.AmbiguousVoyages(
                        $"{voyageIds.Count} voyages correspondent à la destination {idDestination}, au véhicule {idVehicule} et à la date du {jour:dd/MM/yyyy}{detailHeure}. " +
                        "Utilisez l’identifiant unique du voyage ou contactez l’administrateur en cas de doublon.");
                }

                var idVoyage = voyageIds[0];

                var items = await (
                    from e in _context.BilletEmbarquements.AsNoTracking()
                    join p in _context.ReservationPassengers.AsNoTracking() on e.IdReservationPassenger equals p.IdReservationPassenger
                    join r in _context.Reservations.AsNoTracking() on p.IdReservation equals r.IdReservation
                    where r.IdVoyage == idVoyage
                    orderby e.DateEmbarquementUtc
                    select new PassagerEmbarqueVoyageItemDto
                    {
                        IdEmbarquement = e.IdEmbarquement,
                        DateEmbarquementUtc = e.DateEmbarquementUtc,
                        IdBillet = e.IdBillet,
                        IdReservationPassenger = p.IdReservationPassenger,
                        IdReservation = r.IdReservation,
                        IdVoyage = r.IdVoyage,
                        NomComplet = p.NomComplet,
                        Telephone = p.Telephone,
                        IdUtilisateurEnregistrement = e.IdUtilisateurEnregistrement
                    }).ToListAsync();

                return PassagersEmbarquesQueryResult.Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors de la lecture des passagers embarqués (destination {IdDestination}, véhicule {IdVehicule}, date {Date}, heure {Heure})",
                    idDestination, idVehicule, dateDepart, heureDepart);
                throw;
            }
        }
    }
}
