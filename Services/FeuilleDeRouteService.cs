using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.FeuilleDeRoute;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services
{
    public class FeuilleDeRouteService : IFeuilleDeRouteService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<FeuilleDeRouteService> _logger;

        public FeuilleDeRouteService(
            CongoTravelDbContext context,
            ILogger<FeuilleDeRouteService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int?> GetVoyageSocieteIdAsync(int idVoyage, CancellationToken cancellationToken = default)
        {
            if (idVoyage <= 0)
                return null;

            return await _context.Voyages.AsNoTracking()
                .Where(v => v.Id == idVoyage)
                .Select(v => (int?)v.IdSociete)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<FeuilleDeRouteDetailDto> GenererAsync(
            int idVoyage,
            int? idUtilisateurGeneration,
            CancellationToken cancellationToken = default)
        {
            if (idVoyage <= 0)
                throw new ArgumentException("idVoyage invalide.", nameof(idVoyage));

            var voyage = await _context.Voyages
                .AsNoTracking()
                .Include(v => v.Societe)
                .Include(v => v.Destination)
                .Include(v => v.Vehicule)
                .Include(v => v.Site)
                .FirstOrDefaultAsync(v => v.Id == idVoyage, cancellationToken);

            if (voyage == null)
                throw new KeyNotFoundException($"Voyage {idVoyage} introuvable.");

            var passagersSource = await (
                from e in _context.BilletEmbarquements.AsNoTracking()
                join p in _context.ReservationPassengers.AsNoTracking()
                    on e.IdReservationPassenger equals p.IdReservationPassenger
                join r in _context.Reservations.AsNoTracking()
                    on p.IdReservation equals r.IdReservation
                join b in _context.Billets.AsNoTracking()
                    on e.IdBillet equals b.IdBillet into billets
                from b in billets.DefaultIfEmpty()
                where r.IdVoyage == idVoyage
                orderby e.DateEmbarquementUtc
                select new
                {
                    e.IdEmbarquement,
                    e.IdBillet,
                    e.IdReservationPassenger,
                    r.IdReservation,
                    p.NomComplet,
                    p.Telephone,
                    p.Email,
                    p.DocumentType,
                    p.DocumentNumero,
                    CodeSiege = b != null ? b.CodeSiege : null,
                    e.DateEmbarquementUtc,
                    e.IdUtilisateurEnregistrement
                }).ToListAsync(cancellationToken);

            var nowUtc = DateTime.UtcNow;
            var destinationLibelle = voyage.Destination == null
                ? null
                : $"{voyage.Destination.VilleDepart} → {voyage.Destination.VilleArrivee}";

            var feuille = new FeuilleDeRoute
            {
                IdSociete = voyage.IdSociete,
                IdVoyage = voyage.Id,
                DateEmbarquement = voyage.DateDepart.Date,
                DateGenerationUtc = nowUtc,
                IdUtilisateurGeneration = idUtilisateurGeneration,
                SocieteNom = voyage.Societe?.Nom,
                SocieteTelephone = voyage.Societe?.Telephone,
                SocieteEmail = voyage.Societe?.EmailContact,
                SocieteAdresse = voyage.Societe?.AdresseResidence,
                SocieteLogo = voyage.Societe?.Logo,
                VoyageDateDepart = voyage.DateDepart,
                VoyageHeureDepart = voyage.HeureDepart,
                VoyagePrix = voyage.Prix,
                VoyageCodeDevise = voyage.CodeDevisePrix,
                IdDestination = voyage.IdDestination,
                DestinationLibelle = destinationLibelle,
                IdVehicule = voyage.IdVehicule,
                VehiculeImmatriculation = voyage.Vehicule?.NumeroDePlaque,
                VehiculeAlias = voyage.Vehicule?.AliasVehicule,
                IdSite = voyage.IdSite,
                SiteNom = voyage.Site?.NomSite,
                NombrePassagers = passagersSource.Count
            };

            foreach (var src in passagersSource)
            {
                feuille.Passagers.Add(new FeuilleDeRoutePassager
                {
                    IdEmbarquement = src.IdEmbarquement,
                    IdBillet = src.IdBillet,
                    IdReservationPassenger = src.IdReservationPassenger,
                    IdReservation = src.IdReservation,
                    NomComplet = src.NomComplet,
                    Telephone = src.Telephone,
                    Email = src.Email,
                    DocumentType = src.DocumentType,
                    DocumentNumero = src.DocumentNumero,
                    CodeSiege = src.CodeSiege,
                    DateEmbarquementUtc = src.DateEmbarquementUtc,
                    IdUtilisateurEnregistrement = src.IdUtilisateurEnregistrement
                });
            }

            _context.FeuilleDeRoutes.Add(feuille);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "FeuilleDeRoute {Id} générée pour voyage {IdVoyage} ({Count} passagers)",
                feuille.IdFeuilleDeRoute,
                idVoyage,
                feuille.NombrePassagers);

            return MapDetail(feuille);
        }

        public async Task<FeuilleDeRouteDetailDto?> GetByIdAsync(
            int idFeuilleDeRoute,
            CancellationToken cancellationToken = default)
        {
            var feuille = await _context.FeuilleDeRoutes
                .AsNoTracking()
                .Include(f => f.Passagers)
                .FirstOrDefaultAsync(f => f.IdFeuilleDeRoute == idFeuilleDeRoute, cancellationToken);

            return feuille == null ? null : MapDetail(feuille);
        }

        public async Task<PagedResult<FeuilleDeRouteListItemDto>> GetBySocieteAsync(
            int idSociete,
            int? idVoyage,
            DateTime? dateEmbarquement,
            PagedRequest request,
            CancellationToken cancellationToken = default)
        {
            var query = _context.FeuilleDeRoutes.AsNoTracking()
                .Where(f => f.IdSociete == idSociete);

            if (idVoyage.HasValue && idVoyage.Value > 0)
                query = query.Where(f => f.IdVoyage == idVoyage.Value);

            if (dateEmbarquement.HasValue)
            {
                var jour = dateEmbarquement.Value.Date;
                query = query.Where(f => f.DateEmbarquement == jour);
            }

            var total = await query.CountAsync(cancellationToken);

            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

            var items = await query
                .OrderByDescending(f => f.DateGenerationUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FeuilleDeRouteListItemDto
                {
                    IdFeuilleDeRoute = f.IdFeuilleDeRoute,
                    IdSociete = f.IdSociete,
                    IdVoyage = f.IdVoyage,
                    DateEmbarquement = f.DateEmbarquement,
                    DateGenerationUtc = f.DateGenerationUtc,
                    IdUtilisateurGeneration = f.IdUtilisateurGeneration,
                    SocieteNom = f.SocieteNom,
                    DestinationLibelle = f.DestinationLibelle,
                    VoyageDateDepart = f.VoyageDateDepart,
                    VoyageHeureDepart = f.VoyageHeureDepart,
                    VehiculeImmatriculation = f.VehiculeImmatriculation,
                    VehiculeAlias = f.VehiculeAlias,
                    SiteNom = f.SiteNom,
                    NombrePassagers = f.NombrePassagers
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<FeuilleDeRouteListItemDto>(items, total, pageNumber, pageSize);
        }

        public async Task<IReadOnlyList<FeuilleDeRouteListItemDto>> GetByVoyageAsync(
            int idVoyage,
            CancellationToken cancellationToken = default)
        {
            return await _context.FeuilleDeRoutes.AsNoTracking()
                .Where(f => f.IdVoyage == idVoyage)
                .OrderByDescending(f => f.DateGenerationUtc)
                .Select(f => new FeuilleDeRouteListItemDto
                {
                    IdFeuilleDeRoute = f.IdFeuilleDeRoute,
                    IdSociete = f.IdSociete,
                    IdVoyage = f.IdVoyage,
                    DateEmbarquement = f.DateEmbarquement,
                    DateGenerationUtc = f.DateGenerationUtc,
                    IdUtilisateurGeneration = f.IdUtilisateurGeneration,
                    SocieteNom = f.SocieteNom,
                    DestinationLibelle = f.DestinationLibelle,
                    VoyageDateDepart = f.VoyageDateDepart,
                    VoyageHeureDepart = f.VoyageHeureDepart,
                    VehiculeImmatriculation = f.VehiculeImmatriculation,
                    VehiculeAlias = f.VehiculeAlias,
                    SiteNom = f.SiteNom,
                    NombrePassagers = f.NombrePassagers
                })
                .ToListAsync(cancellationToken);
        }

        private static FeuilleDeRouteDetailDto MapDetail(FeuilleDeRoute f) => new()
        {
            IdFeuilleDeRoute = f.IdFeuilleDeRoute,
            IdSociete = f.IdSociete,
            IdVoyage = f.IdVoyage,
            DateEmbarquement = f.DateEmbarquement,
            DateGenerationUtc = f.DateGenerationUtc,
            IdUtilisateurGeneration = f.IdUtilisateurGeneration,
            SocieteNom = f.SocieteNom,
            SocieteTelephone = f.SocieteTelephone,
            SocieteEmail = f.SocieteEmail,
            SocieteAdresse = f.SocieteAdresse,
            SocieteLogo = f.SocieteLogo,
            VoyageDateDepart = f.VoyageDateDepart,
            VoyageHeureDepart = f.VoyageHeureDepart,
            VoyagePrix = f.VoyagePrix,
            VoyageCodeDevise = f.VoyageCodeDevise,
            IdDestination = f.IdDestination,
            DestinationLibelle = f.DestinationLibelle,
            IdVehicule = f.IdVehicule,
            VehiculeImmatriculation = f.VehiculeImmatriculation,
            VehiculeAlias = f.VehiculeAlias,
            IdSite = f.IdSite,
            SiteNom = f.SiteNom,
            NombrePassagers = f.NombrePassagers,
            Passagers = f.Passagers
                .OrderBy(p => p.DateEmbarquementUtc)
                .ThenBy(p => p.IdFeuilleDeRoutePassager)
                .Select(p => new FeuilleDeRoutePassagerDto
                {
                    IdFeuilleDeRoutePassager = p.IdFeuilleDeRoutePassager,
                    IdEmbarquement = p.IdEmbarquement,
                    IdBillet = p.IdBillet,
                    IdReservationPassenger = p.IdReservationPassenger,
                    IdReservation = p.IdReservation,
                    NomComplet = p.NomComplet,
                    Telephone = p.Telephone,
                    Email = p.Email,
                    DocumentType = p.DocumentType,
                    DocumentNumero = p.DocumentNumero,
                    CodeSiege = p.CodeSiege,
                    DateEmbarquementUtc = p.DateEmbarquementUtc,
                    IdUtilisateurEnregistrement = p.IdUtilisateurEnregistrement
                })
                .ToList()
        };
    }
}
