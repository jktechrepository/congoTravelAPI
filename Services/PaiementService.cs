using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models.Enums;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service de gestion des paiements avec émission automatique de billets
    /// </summary>
    public class PaiementService : IPaiementRepository
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<PaiementService> _logger;
        private readonly BilletEmissionService _billetEmissionService;

        public PaiementService(
            CongoTravelDbContext context, 
            ILogger<PaiementService> logger,
            BilletEmissionService billetEmissionService)
        {
            _context = context;
            _logger = logger;
            _billetEmissionService = billetEmissionService;
        }

        private IQueryable<Paiement> QueryPaiementsForRead() =>
            _context.Paiements
                .Include(p => p.Utilisateur)
                .Include(p => p.Societe)
                .Include(p => p.Reservation!)
                    .ThenInclude(r => r.Client)
                .Where(p => p.IsDeleted == false);

        /// <summary>
        /// Récupérer tous les paiements
        /// </summary>
        public async Task<IEnumerable<Paiement>> GetAllAsync()
        {
            return await QueryPaiementsForRead()
                .OrderByDescending(p => p.DateCreation)
                .ToListAsync();
        }

        /// <summary>
        /// Récupérer un paiement par son ID
        /// </summary>
        public async Task<Paiement?> GetByIdAsync(int id)
        {
            return await QueryPaiementsForRead()
                .FirstOrDefaultAsync(p => p.IdPaiement == id);
        }

        /// <summary>
        /// Récupérer les paiements par réservation
        /// </summary>
        public async Task<IEnumerable<Paiement>> GetByReservationAsync(int idReservation)
        {
            return await QueryPaiementsForRead()
                .Where(p => p.IdReservation == idReservation)
                .OrderByDescending(p => p.DateCreation)
                .ToListAsync();
        }

        /// <summary>
        /// Récupérer les paiements par utilisateur
        /// </summary>
        public async Task<IEnumerable<Paiement>> GetByUtilisateurAsync(int idUtilisateur)
        {
            return await QueryPaiementsForRead()
                .Where(p => p.IdUtilisateur == idUtilisateur)
                .OrderByDescending(p => p.DateCreation)
                .ToListAsync();
        }

        /// <summary>
        /// Récupérer les paiements par société
        /// </summary>
        public async Task<IEnumerable<Paiement>> GetBySocieteAsync(int idSociete)
        {
            return await QueryPaiementsForRead()
                .Where(p => p.IdSociete == idSociete)
                .OrderByDescending(p => p.DateCreation)
                .ToListAsync();
        }

        /// <summary>
        /// Récupérer les paiements paginés
        /// </summary>
        public async Task<PagedResult<Paiement>> GetPagedAsync(PaiementPagedRequest request)
        {
            request ??= new PaiementPagedRequest();

            var query = ApplyPaiementPagedFilters(QueryPaiementsForRead(), request);

            query = request.SortBy switch
            {
                "DateCreation" or "date" => request.SortDescending 
                    ? query.OrderByDescending(p => p.DateCreation) 
                    : query.OrderBy(p => p.DateCreation),
                "MontantPaye" or "montant" => request.SortDescending 
                    ? query.OrderByDescending(p => p.MontantPaye) 
                    : query.OrderBy(p => p.MontantPaye),
                "Statut" or "statut" => request.SortDescending 
                    ? query.OrderByDescending(p => p.Statut) 
                    : query.OrderBy(p => p.Statut),
                "MethodePaiement" or "methode" => request.SortDescending 
                    ? query.OrderByDescending(p => p.MethodePaiement) 
                    : query.OrderBy(p => p.MethodePaiement),
                _ => query.OrderByDescending(p => p.DateCreation)
            };

            var total = await query.CountAsync();
            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<Paiement>(data, total, request.PageNumber, request.PageSize);
        }

        /// <summary>
        /// Créer un nouveau paiement avec émission automatique de billet si complet
        /// </summary>
        public async Task<Paiement> CreateAsync(Paiement paiement)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // 1. Création du paiement
                paiement.DateCreation = DateTime.Now;
                paiement.Statut = true;
                paiement.MethodePaiement = MethodePaiementHelper.NormalizeForStorage(paiement.MethodePaiement);
                paiement.StatutPaiementMetier ??= (int)StatutPaiementMetier.Reussi;

                if (paiement.IdReservation.HasValue
                    && (string.IsNullOrWhiteSpace(paiement.Origine)
                        || paiement.Origine == OrigineOperation.INCONNU))
                {
                    var reservationOrigine = await _context.Reservations.AsNoTracking()
                        .Where(r => r.IdReservation == paiement.IdReservation.Value)
                        .Select(r => r.Origine)
                        .FirstOrDefaultAsync();
                    if (!string.IsNullOrWhiteSpace(reservationOrigine)
                        && reservationOrigine != OrigineOperation.INCONNU)
                    {
                        paiement.Origine = reservationOrigine;
                    }
                }

                _context.Paiements.Add(paiement);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Paiement créé avec succès - ID: {Id}, Montant: {Montant}, Réservation: {Reservation}", 
                    paiement.IdPaiement, paiement.MontantPaye, paiement.IdReservation);

                // 2. Émission automatique du billet si paiement complet
                if (paiement.EstComplet)
                {
                    await EmettreBilletAutomatiqueAsync(paiement);
                }

                await transaction.CommitAsync();
                return paiement;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erreur lors de la création du paiement {Id}", paiement.IdPaiement);
                throw;
            }
        }

        /// <summary>
        /// Émet un billet automatiquement pour un paiement complet
        /// </summary>
        private async Task EmettreBilletAutomatiqueAsync(Paiement paiement)
        {
            try
            {
                _logger.LogInformation("Tentative d'émission billet automatique - Paiement: {PaiementId}", 
                    paiement.IdPaiement);

                // Vérifier si un billet peut être émis
                if (await _billetEmissionService.PeutEmettreBilletAsync(paiement))
                {
                    var billets = await _billetEmissionService.EmitBilletsPourPaiementAsync(paiement);
                    var premier = billets.Count > 0 ? billets[0] : null;

                    if (premier != null)
                    {
                        paiement.DateEmissionBillet = DateTime.UtcNow;
                        paiement.IdBilletEmis = premier.IdBillet;
                        paiement.BilletEmis = premier;

                        await _context.SaveChangesAsync();

                        _logger.LogInformation(
                            "Billet(s) émis automatiquement - Paiement: {PaiementId}, Premier billet: {BilletId}, Nombre: {Count}, QR: {QrCode}",
                            paiement.IdPaiement,
                            premier.IdBillet,
                            billets.Count,
                            premier.QrCode);
                    }
                }
                else
                {
                    _logger.LogWarning("Impossible d'émettre un billet pour le paiement {PaiementId} - conditions non remplies", 
                        paiement.IdPaiement);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'émission automatique du billet pour paiement {PaiementId}", 
                    paiement.IdPaiement);
                
                // Ne pas propager l'erreur pour ne pas bloquer la création du paiement
                // Le paiement est créé, mais le billet n'est pas émis
                _logger.LogInformation("Paiement {PaiementId} créé mais billet non émis - vérification manuelle requise", 
                    paiement.IdPaiement);
            }
        }

        /// <summary>
        /// Mettre à jour un paiement existant
        /// </summary>
        public async Task<Paiement> UpdateAsync(Paiement paiement)
        {
            var existing = await _context.Paiements
                .FirstOrDefaultAsync(p => p.IdPaiement == paiement.IdPaiement && p.IsDeleted == false);

            if (existing == null)
                throw new InvalidOperationException("Paiement non trouvé");

            _context.Entry(existing).CurrentValues.SetValues(paiement);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Paiement mis à jour - ID: {Id}", paiement.IdPaiement);

            return existing;
        }

        /// <summary>
        /// Supprimer un paiement (soft delete)
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var paiement = await _context.Paiements
                .FirstOrDefaultAsync(p => p.IdPaiement == id && p.IsDeleted == false);

            if (paiement == null)
                return false;

            paiement.IsDeleted = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Paiement supprimé - ID: {Id}", id);

            return true;
        }

        /// <summary>
        /// Récupérer les paiements paginés par société
        /// </summary>
        public async Task<PagedResult<Paiement>> GetBySocietePagedAsync(int idSociete, PaiementPagedRequest request)
        {
            request ??= new PaiementPagedRequest();

            var query = ApplyPaiementPagedFilters(
                QueryPaiementsForRead().Where(p => p.IdSociete == idSociete),
                request);

            query = request.SortBy switch
            {
                "DateCreation" => request.SortDescending ? query.OrderByDescending(p => p.DateCreation) : query.OrderBy(p => p.DateCreation),
                "MontantPaye" or "montant" => request.SortDescending ? query.OrderByDescending(p => p.MontantPaye) : query.OrderBy(p => p.MontantPaye),
                "Statut" => request.SortDescending ? query.OrderByDescending(p => p.Statut) : query.OrderBy(p => p.Statut),
                _ => query.OrderByDescending(p => p.DateCreation)
            };

            var total = await query.CountAsync();
            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<Paiement>(data, total, request.PageNumber, request.PageSize);
        }

        /// <summary>
        /// Récupérer le nombre total de paiements
        /// </summary>
        public async Task<int> GetTotalCountAsync()
        {
            return await _context.Paiements
                .Where(p => p.IsDeleted == false)
                .CountAsync();
        }

        /// <summary>
        /// Vérifier si un paiement existe
        /// </summary>
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Paiements
                .AnyAsync(p => p.IdPaiement == id && p.IsDeleted == false);
        }

        /// <summary>
        /// Récupérer les paiements par facture (obsolète - pour compatibilité)
        /// </summary>
        public async Task<IEnumerable<Paiement>> GetByFactureAsync(int idFacture)
        {
            // Les factures ne sont plus utilisées dans le nouveau workflow
            // Retourner une liste vide pour maintenir la compatibilité
            _logger.LogWarning("GetByFactureAsync appelé avec ID {Id} - méthode obsolète dans le nouveau workflow", idFacture);
            return new List<Paiement>();
        }

        /// <summary>
        /// Paiements liés aux réservations du client (<see cref="Reservation.IdClient"/>).
        /// </summary>
        public async Task<IEnumerable<Paiement>> GetByClientAsync(int idClient)
        {
            return await QueryPaiementsForRead()
                .Where(p => p.IdReservation != null
                            && p.Reservation != null
                            && p.Reservation.IdClient == idClient)
                .OrderByDescending(p => p.DateCreation)
                .ToListAsync();
        }

        /// <summary>
        /// Récupérer le total des paiements par facture (obsolète - pour compatibilité)
        /// </summary>
        public async Task<decimal> GetTotalPaiementsByFactureAsync(int idFacture)
        {
            // Les factures ne sont plus utilisées dans le nouveau workflow
            _logger.LogWarning("GetTotalPaiementsByFactureAsync appelé avec ID {Id} - méthode obsolète dans le nouveau workflow", idFacture);
            return 0;
        }

        private static IQueryable<Paiement> ApplyPaiementPagedFilters(
            IQueryable<Paiement> query,
            PaiementPagedRequest request)
        {
            query = OrigineOperationGroupeHelper.ApplyOrigineGroupeFilter(query, request.OrigineGroupe);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLowerInvariant();
                query = query.Where(p =>
                    (p.Reservation != null && $"RES-{p.Reservation.IdReservation:D6}".ToLowerInvariant().Contains(term)) ||
                    (p.Utilisateur != null && p.Utilisateur.NomComplet.ToLowerInvariant().Contains(term)) ||
                    (p.Reservation != null && p.Reservation.Client != null && p.Reservation.Client.NomClient.ToLowerInvariant().Contains(term)) ||
                    (p.MethodePaiement != null && p.MethodePaiement.ToLowerInvariant().Contains(term)));
            }

            return query;
        }
    }
}
