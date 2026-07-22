using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Communication;
using CongoTravel.Services.Notifications;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service d'exécution des campagnes de communication
    /// </summary>
    public class CommunicationDispatchService : ICommunicationDispatchService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IClientFilterService _clientFilterService;
        private readonly INotificationSender _notificationSender;
        private readonly ILogger<CommunicationDispatchService> _logger;

        public CommunicationDispatchService(
            CongoTravelDbContext context,
            IClientFilterService clientFilterService,
            INotificationSender notificationSender,
            ILogger<CommunicationDispatchService> logger)
        {
            _context = context;
            _clientFilterService = clientFilterService;
            _notificationSender = notificationSender;
            _logger = logger;
        }

        public async Task<CommunicationCampaignDispatchResult> ExecuteCampaignAsync(int idCampagne)
        {
            var result = new CommunicationCampaignDispatchResult
            {
                IdCampagne = idCampagne,
                DateExecution = DateTime.Now
            };

            try
            {
                var campaign = await _context.CommunicationCampaigns
                    .Include(c => c.Societe)
                    .FirstOrDefaultAsync(c => c.IdCampagne == idCampagne);

                if (campaign == null)
                {
                    result.MessageErreur = $"Campagne {idCampagne} introuvable";
                    return result;
                }

                // Vérifier que la campagne n'est pas déjà terminée
                if (campaign.EstTerminee)
                {
                    result.MessageErreur = "La campagne est déjà terminée";
                    return result;
                }

                // Marquer la campagne comme en cours
                campaign.EstEnCours = true;
                campaign.EstProgrammee = false;
                await _context.SaveChangesAsync();

                // Récupérer les clients ciblés
                List<Client> clients;
                if (!string.IsNullOrWhiteSpace(campaign.ListeIdClients))
                {
                    // Liste spécifique d'IDs clients
                    try
                    {
                        var ids = JsonSerializer.Deserialize<int[]>(campaign.ListeIdClients);
                        clients = await _context.Clients
                            .Where(c => c.Statut == true && ids != null && ids.Contains(c.IdClient))
                            .ToListAsync();
                    }
                    catch
                    {
                        clients = new List<Client>();
                    }
                }
                else if (!string.IsNullOrWhiteSpace(campaign.CriteresCiblage))
                {
                    // Critères de ciblage
                    try
                    {
                        var criteres = JsonSerializer.Deserialize<CriteresCiblageDto>(campaign.CriteresCiblage);
                        clients = await _clientFilterService.GetClientsByCriteriaAsync(criteres);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Erreur lors de la désérialisation des critères de ciblage");
                        clients = new List<Client>();
                    }
                }
                else
                {
                    // Aucun critère, tous les clients actifs
                    clients = await _context.Clients
                        .Where(c => c.Statut == true)
                        .ToListAsync();
                }

                result.NombreDestinataires = clients.Count;
                campaign.NombreDestinataires = clients.Count;

                _logger.LogInformation(
                    "📢 Début d'exécution de la campagne {CampaignId} - {Titre} - {Count} client(s) ciblé(s)",
                    campaign.IdCampagne, campaign.Titre, clients.Count);

                // Statistiques par canal
                var statsParCanal = new Dictionary<string, int>
                {
                    ["Push"] = 0,
                    ["Sms"] = 0,
                    ["Email"] = 0,
                    ["InApp"] = 0
                };

                int succes = 0;
                int echecs = 0;
                int envoyes = 0;

                // Pour chaque client, envoyer la notification
                foreach (var client in clients)
                {
                    // Récupérer les utilisateurs associés au client
                    var utilisateurs = await _context.Utilisateurs
                        .Where(u => u.IdClient == client.IdClient && u.Statut == true)
                        .ToListAsync();

                    if (!utilisateurs.Any())
                    {
                        _logger.LogWarning("⚠️ Client {ClientId} ({NomClient}) n'a aucun utilisateur actif associé", 
                            client.IdClient, client.NomClient);
                        continue;
                    }

                    // Envoyer à chaque utilisateur du client
                    foreach (var utilisateur in utilisateurs)
                    {
                        try
                        {
                            envoyes++;

                            // Construire le message de notification
                            var message = new NotificationMessage
                            {
                                Push = campaign.ActiverPush ? new PushNotificationMessage
                                {
                                    Title = campaign.Titre,
                                    Body = campaign.Contenu,
                                    Type = campaign.TypeCampagne,
                                    Data = new Dictionary<string, string>
                                    {
                                        ["type"] = "COMMUNICATION",
                                        ["campaignId"] = campaign.IdCampagne.ToString(),
                                        ["campaignType"] = campaign.TypeCampagne
                                    },
                                    IsEnabled = true
                                } : null,
                                Sms = campaign.ActiverSms ? new SmsNotificationMessage
                                {
                                    Body = $"{campaign.Titre}\n\n{campaign.Contenu}",
                                    IsEnabled = true
                                } : null,
                                Email = campaign.ActiverEmail ? new EmailNotificationMessage
                                {
                                    Subject = campaign.Titre,
                                    PlainTextBody = campaign.Contenu,
                                    HtmlBody = $"<h2>{campaign.Titre}</h2><p>{campaign.Contenu.Replace("\n", "<br/>")}</p>",
                                    IsEnabled = true
                                } : null,
                                InApp = campaign.ActiverInApp ? new InAppNotificationMessage
                                {
                                    Title = campaign.Titre,
                                    Content = campaign.Contenu,
                                    Type = campaign.TypeCampagne,
                                    Metadata = new Dictionary<string, string>
                                    {
                                        ["campaignId"] = campaign.IdCampagne.ToString(),
                                        ["campaignType"] = campaign.TypeCampagne
                                    },
                                    IsEnabled = true
                                } : null
                            };

                            // Construire le contexte
                            var context = new NotificationContext
                            {
                                Kind = NotificationKind.Communication,
                                UtilisateurDestinataire = utilisateur,
                                Societe = campaign.Societe,
                                AcceptsSms = true, // Par défaut, accepter les SMS
                                AllowPush = campaign.ActiverPush,
                                AllowInApp = campaign.ActiverInApp,
                                AllowSms = campaign.ActiverSms,
                                UtilisateurActif = utilisateur.Statut == true
                            };

                            var dispatchResult = new NotificationDispatchResult(context, message);
                            await _notificationSender.SendAsync(dispatchResult);

                            succes++;

                            // Mettre à jour les statistiques par canal
                            if (campaign.ActiverPush) statsParCanal["Push"]++;
                            if (campaign.ActiverSms) statsParCanal["Sms"]++;
                            if (campaign.ActiverEmail) statsParCanal["Email"]++;
                            if (campaign.ActiverInApp) statsParCanal["InApp"]++;
                        }
                        catch (Exception ex)
                        {
                            echecs++;
                            _logger.LogError(ex, 
                                "❌ Erreur lors de l'envoi de la notification pour le client {ClientId} / utilisateur {UserId}",
                                client.IdClient, utilisateur.IdUtilisateur);
                        }
                    }
                }

                // Mettre à jour les statistiques de la campagne
                campaign.NombreEnvoyes = envoyes;
                campaign.NombreSucces = succes;
                campaign.NombreEchecs = echecs;
                campaign.EstEnCours = false;
                campaign.EstTerminee = true;
                campaign.DateEnvoiEffectif = DateTime.Now;

                await _context.SaveChangesAsync();

                result.NombreEnvoyes = envoyes;
                result.NombreSucces = succes;
                result.NombreEchecs = echecs;
                result.StatistiquesParCanal = statsParCanal;

                _logger.LogInformation(
                    "✅ Campagne {CampaignId} terminée - Envoyés: {Envoyes}, Succès: {Succes}, Échecs: {Echecs}",
                    campaign.IdCampagne, envoyes, succes, echecs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'exécution de la campagne {CampaignId}", idCampagne);
                result.MessageErreur = ex.Message;

                // Marquer la campagne comme non en cours en cas d'erreur
                var campaign = await _context.CommunicationCampaigns.FindAsync(idCampagne);
                if (campaign != null)
                {
                    campaign.EstEnCours = false;
                    await _context.SaveChangesAsync();
                }
            }

            return result;
        }

        public async Task<List<Client>> PreviewTargetedClientsAsync(int idCampagne)
        {
            var campaign = await _context.CommunicationCampaigns
                .FirstOrDefaultAsync(c => c.IdCampagne == idCampagne);

            if (campaign == null)
                return new List<Client>();

            if (!string.IsNullOrWhiteSpace(campaign.ListeIdClients))
            {
                try
                {
                    var ids = JsonSerializer.Deserialize<int[]>(campaign.ListeIdClients);
                    return await _context.Clients
                        .Where(c => c.Statut == true && ids != null && ids.Contains(c.IdClient))
                        .ToListAsync();
                }
                catch
                {
                    return new List<Client>();
                }
            }
            else if (!string.IsNullOrWhiteSpace(campaign.CriteresCiblage))
            {
                try
                {
                    var criteres = JsonSerializer.Deserialize<CriteresCiblageDto>(campaign.CriteresCiblage);
                    return await _clientFilterService.GetClientsByCriteriaAsync(criteres);
                }
                catch
                {
                    return new List<Client>();
                }
            }
            else
            {
                return await _context.Clients
                    .Where(c => c.Statut == true)
                    .ToListAsync();
            }
        }
    }
}

