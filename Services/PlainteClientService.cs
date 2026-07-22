using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service de gestion des plaintes clients
    /// </summary>
    public class PlainteClientService : IPlainteClientRepository
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<PlainteClientService> _logger;

        public PlainteClientService(
            CongoTravelDbContext context,
            ILogger<PlainteClientService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<PlainteClient>> GetAllAsync()
        {
            return await _context.PlainteClients
                .Include(p => p.Client)
                .Include(p => p.AgentAssigné)
                .Include(p => p.UtilisateurCreateur)
                .Where(p => p.Statut == true)
                .OrderByDescending(p => p.DateCreation)
                .ToListAsync();
        }

        public async Task<PagedResult<PlainteClient>> GetPagedAsync(
            PagedRequest request, 
            string? statut = null, 
            string? priorite = null, 
            int? idAgent = null, 
            int? idClient = null)
        {
            request ??= new PagedRequest();

            var query = _context.PlainteClients
                .Include(p => p.Client)
                .Include(p => p.AgentAssigné)
                .Include(p => p.UtilisateurCreateur)
                .Where(p => p.Statut == true)
                .AsQueryable();

            // Filtre par statut
            if (!string.IsNullOrWhiteSpace(statut))
            {
                query = query.Where(p => p.StatutPlainte == statut);
            }

            // Filtre par priorité
            if (!string.IsNullOrWhiteSpace(priorite))
            {
                query = query.Where(p => p.Priorite == priorite);
            }

            // Filtre par agent assigné
            if (idAgent.HasValue)
            {
                query = query.Where(p => p.IdAgentAssigné == idAgent.Value);
            }

            // Filtre par client
            if (idClient.HasValue)
            {
                query = query.Where(p => p.IdClient == idClient.Value);
            }

            // Note: Le filtrage par zone a été retiré car le champ Zone n'existe plus dans le modèle Client

            // Recherche textuelle
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(p => 
                    (p.Titre != null && p.Titre.ToLower().Contains(term)) ||
                    (p.Description != null && p.Description.ToLower().Contains(term)) ||
                    (p.TypePanne != null && p.TypePanne.ToLower().Contains(term)));
            }

            // Tri
            query = request.SortBy?.ToLower() switch
            {
                "titre" => request.SortDescending 
                    ? query.OrderByDescending(p => p.Titre) 
                    : query.OrderBy(p => p.Titre),
                "datecreation" => request.SortDescending 
                    ? query.OrderByDescending(p => p.DateCreation) 
                    : query.OrderBy(p => p.DateCreation),
                "statut" => request.SortDescending 
                    ? query.OrderByDescending(p => p.StatutPlainte) 
                    : query.OrderBy(p => p.StatutPlainte),
                "priorite" => request.SortDescending 
                    ? query.OrderByDescending(p => p.Priorite) 
                    : query.OrderBy(p => p.Priorite),
                _ => request.SortDescending 
                    ? query.OrderByDescending(p => p.DateCreation) 
                    : query.OrderBy(p => p.DateCreation)
            };

            var total = await query.CountAsync();
            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<PlainteClient>(data, total, request.PageNumber, request.PageSize);
        }

        public async Task<PlainteClient?> GetByIdAsync(int id)
        {
            return await _context.PlainteClients
                .Include(p => p.Client)
                .Include(p => p.AgentAssigné)
                .Include(p => p.UtilisateurCreateur)
                .Where(p => p.Statut == true)
                .FirstOrDefaultAsync(p => p.IdPlainte == id);
        }

        public async Task<IEnumerable<PlainteClient>> GetByClientAsync(int idClient)
        {
            return await _context.PlainteClients
                .Include(p => p.Client)
                .Include(p => p.AgentAssigné)
                .Where(p => p.IdClient == idClient && p.Statut == true)
                .OrderByDescending(p => p.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<PlainteClient>> GetEnAttenteAsync()
        {
            return await _context.PlainteClients
                .Include(p => p.Client)
                .Include(p => p.AgentAssigné)
                .Where(p => p.StatutPlainte == "En attente" && p.Statut == true)
                .OrderByDescending(p => p.EstUrgente)
                .ThenByDescending(p => p.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<PlainteClient>> GetByAgentAsync(int idAgent)
        {
            return await _context.PlainteClients
                .Include(p => p.Client)
                .Include(p => p.AgentAssigné)
                .Where(p => p.IdAgentAssigné == idAgent && p.Statut == true)
                .OrderByDescending(p => p.DateCreation)
                .ToListAsync();
        }

        public async Task<PlainteClient> CreateAsync(PlainteClient plainte)
        {
            // Vérifier que le client existe
            var client = await _context.Clients.FindAsync(plainte.IdClient);
            if (client == null)
            {
                throw new InvalidOperationException($"Client {plainte.IdClient} introuvable");
            }

            // Récupérer l'utilisateur associé au client (premier utilisateur actif)
            if (!plainte.IdUtilisateurCreateur.HasValue)
            {
                var utilisateur = await _context.Utilisateurs
                    .FirstOrDefaultAsync(u => u.IdClient == plainte.IdClient && u.Statut == true);
                
                if (utilisateur != null)
                {
                    plainte.IdUtilisateurCreateur = utilisateur.IdUtilisateur;
                }
            }

            plainte.DateCreation = DateTime.Now;
            plainte.DateDerniereModification = DateTime.Now;
            plainte.StatutPlainte = "En attente";

            _context.PlainteClients.Add(plainte);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Plainte client créée: {PlainteId} - Client: {ClientId} - Titre: {Titre}", 
                plainte.IdPlainte, plainte.IdClient, plainte.Titre);

            return plainte;
        }

        public async Task<PlainteClient> UpdateAsync(PlainteClient plainte)
        {
            var existing = await _context.PlainteClients.FindAsync(plainte.IdPlainte);
            if (existing == null)
                throw new InvalidOperationException($"Plainte {plainte.IdPlainte} introuvable");

            existing.Titre = plainte.Titre;
            existing.Description = plainte.Description;
            existing.TypePanne = plainte.TypePanne;
            existing.NiveauImportance = plainte.NiveauImportance;
            existing.RisquesPrincipaux = plainte.RisquesPrincipaux;
            existing.StatutPlainte = plainte.StatutPlainte;
            existing.Priorite = plainte.Priorite;
            existing.IdAgentAssigné = plainte.IdAgentAssigné;
            existing.CommentaireResolution = plainte.CommentaireResolution;
            existing.DateResolution = plainte.DateResolution;
            existing.EstUrgente = plainte.EstUrgente;
            existing.DateDerniereModification = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Plainte client mise à jour: {PlainteId}", plainte.IdPlainte);

            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var plainte = await _context.PlainteClients.FindAsync(id);
            if (plainte == null)
                return false;

            // ✨ Soft delete : mettre Statut à false au lieu de supprimer
            plainte.Statut = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Plainte client désactivée (soft delete): {PlainteId}", id);
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.PlainteClients.AnyAsync(p => p.IdPlainte == id);
        }
    }
}

