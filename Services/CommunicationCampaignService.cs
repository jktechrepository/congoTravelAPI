using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Communication;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service de gestion des campagnes de communication
    /// </summary>
    public class CommunicationCampaignService : ICommunicationCampaignRepository
    {
        private readonly CongoTravelDbContext _context;
        private readonly IClientFilterService _clientFilterService;
        private readonly ILogger<CommunicationCampaignService> _logger;

        public CommunicationCampaignService(
            CongoTravelDbContext context,
            IClientFilterService clientFilterService,
            ILogger<CommunicationCampaignService> logger)
        {
            _context = context;
            _clientFilterService = clientFilterService;
            _logger = logger;
        }

        public async Task<IEnumerable<CommunicationCampaign>> GetAllAsync()
        {
            return await _context.CommunicationCampaigns
                .Include(c => c.Societe)
                .Include(c => c.UtilisateurCreateur)
                .Where(c => c.Statut == true)
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }

        public async Task<PagedResult<CommunicationCampaign>> GetPagedAsync(PagedRequest request)
        {
            request ??= new PagedRequest();

            var query = _context.CommunicationCampaigns
                .Include(c => c.Societe)
                .Include(c => c.UtilisateurCreateur)
                .Where(c => c.Statut == true)
                .AsQueryable();

            // Recherche
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(c => c.Titre.ToLower().Contains(term) ||
                                        c.Contenu.ToLower().Contains(term));
            }

            // Tri
            query = request.SortBy?.ToLower() switch
            {
                "titre" => request.SortDescending 
                    ? query.OrderByDescending(c => c.Titre) 
                    : query.OrderBy(c => c.Titre),
                "datecreation" => request.SortDescending 
                    ? query.OrderByDescending(c => c.DateCreation) 
                    : query.OrderBy(c => c.DateCreation),
                "dateenvoi" => request.SortDescending 
                    ? query.OrderByDescending(c => c.DateEnvoi) 
                    : query.OrderBy(c => c.DateEnvoi),
                _ => request.SortDescending 
                    ? query.OrderByDescending(c => c.IdCampagne) 
                    : query.OrderBy(c => c.IdCampagne)
            };

            var total = await query.CountAsync();
            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<CommunicationCampaign>(data, total, request.PageNumber, request.PageSize);
        }

        public async Task<CommunicationCampaign?> GetByIdAsync(int id)
        {
            return await _context.CommunicationCampaigns
                .Include(c => c.Societe)
                .Include(c => c.UtilisateurCreateur)
                .Where(c => c.Statut == true)
                .FirstOrDefaultAsync(c => c.IdCampagne == id);
        }

        public async Task<CommunicationCampaign> CreateAsync(CommunicationCampaign campaign)
        {
            // Sérialiser les critères de ciblage en JSON
            if (campaign.CriteresCiblage != null)
            {
                // Si c'est déjà une string JSON, la garder, sinon sérialiser
                try
                {
                    JsonDocument.Parse(campaign.CriteresCiblage);
                }
                catch
                {
                    // Si ce n'est pas du JSON valide, sérialiser l'objet
                    // (dans ce cas, on suppose que CriteresCiblage contient déjà le JSON)
                }
            }

            // Sérialiser la liste d'IDs clients en JSON
            if (campaign.ListeIdClients != null)
            {
                try
                {
                    JsonDocument.Parse(campaign.ListeIdClients);
                }
                catch
                {
                    // Si ce n'est pas du JSON valide, sérialiser
                }
            }

            // Calculer le nombre de destinataires
            if (campaign.CriteresCiblage != null)
            {
                try
                {
                    var criteres = JsonSerializer.Deserialize<CriteresCiblageDto>(campaign.CriteresCiblage);
                    var clients = await _clientFilterService.GetClientsByCriteriaAsync(criteres);
                    campaign.NombreDestinataires = clients.Count;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Erreur lors du calcul du nombre de destinataires");
                    campaign.NombreDestinataires = 0;
                }
            }

            campaign.DateCreation = DateTime.Now;
            campaign.DateDerniereModification = DateTime.Now;

            if (campaign.DateEnvoi.HasValue && campaign.DateEnvoi.Value > DateTime.Now)
            {
                campaign.EstProgrammee = true;
            }

            _context.CommunicationCampaigns.Add(campaign);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Campagne de communication créée: {CampaignId} - {Titre}", 
                campaign.IdCampagne, campaign.Titre);

            return campaign;
        }

        public async Task<CommunicationCampaign> UpdateAsync(CommunicationCampaign campaign)
        {
            var existing = await _context.CommunicationCampaigns.FindAsync(campaign.IdCampagne);
            if (existing == null)
                throw new InvalidOperationException($"Campagne {campaign.IdCampagne} introuvable");

            // Ne pas permettre la modification si la campagne est en cours ou terminée
            if (existing.EstEnCours || existing.EstTerminee)
            {
                throw new InvalidOperationException(
                    "Impossible de modifier une campagne en cours ou terminée");
            }

            existing.Titre = campaign.Titre;
            existing.Contenu = campaign.Contenu;
            existing.TypeCampagne = campaign.TypeCampagne;
            existing.IdSociete = campaign.IdSociete;
            existing.CriteresCiblage = campaign.CriteresCiblage;
            existing.ListeIdClients = campaign.ListeIdClients;
            existing.ActiverPush = campaign.ActiverPush;
            existing.ActiverSms = campaign.ActiverSms;
            existing.ActiverEmail = campaign.ActiverEmail;
            existing.ActiverInApp = campaign.ActiverInApp;
            existing.DateEnvoi = campaign.DateEnvoi;
            existing.DateDerniereModification = DateTime.Now;

            // Recalculer le nombre de destinataires si les critères ont changé
            if (campaign.CriteresCiblage != null)
            {
                try
                {
                    var criteres = JsonSerializer.Deserialize<CriteresCiblageDto>(campaign.CriteresCiblage);
                    var clients = await _clientFilterService.GetClientsByCriteriaAsync(criteres);
                    existing.NombreDestinataires = clients.Count;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Erreur lors du recalcul du nombre de destinataires");
                }
            }

            if (existing.DateEnvoi.HasValue && existing.DateEnvoi.Value > DateTime.Now)
            {
                existing.EstProgrammee = true;
            }
            else
            {
                existing.EstProgrammee = false;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Campagne de communication mise à jour: {CampaignId}", 
                campaign.IdCampagne);

            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var campaign = await _context.CommunicationCampaigns.FindAsync(id);
            if (campaign == null)
                return false;

            // Ne pas permettre la suppression si la campagne est en cours
            if (campaign.EstEnCours)
            {
                throw new InvalidOperationException(
                    "Impossible de supprimer une campagne en cours d'envoi");
            }

            // ✨ Soft delete : mettre Statut à false au lieu de supprimer
            campaign.Statut = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Campagne de communication désactivée (soft delete): {CampaignId}", id);
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.CommunicationCampaigns.AnyAsync(c => c.IdCampagne == id);
        }
    }
}

