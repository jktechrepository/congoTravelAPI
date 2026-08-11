using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Enums;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Services.Restaurant
{
    public class RestaurantCreneauGenerationService : IRestaurantCreneauGenerationService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IRestaurantCreneauService _creneauService;
        private readonly ILogger<RestaurantCreneauGenerationService> _logger;

        public RestaurantCreneauGenerationService(
            CongoTravelDbContext context,
            IRestaurantCreneauService creneauService,
            ILogger<RestaurantCreneauGenerationService> logger)
        {
            _context = context;
            _creneauService = creneauService;
            _logger = logger;
        }

        public async Task<RestaurantPlanificationGenerationResultDto> GenererAsync(
            int idPlanification,
            GenererRestaurantPlanificationDto request,
            int? declencheParIdUtilisateur = null,
            CancellationToken cancellationToken = default)
        {
            var planif = await _context.RestaurantPlanifications.AsNoTracking()
                .Include(p => p.Plages)
                    .ThenInclude(pl => pl.GlobalQuota)
                .Include(p => p.Plages)
                    .ThenInclude(pl => pl.ZoneQuotas)
                .FirstOrDefaultAsync(p => p.IdRestaurantPlanification == idPlanification, cancellationToken);

            if (planif == null)
                throw new KeyNotFoundException($"Planification {idPlanification} introuvable.");

            if (!planif.Statut)
                throw new InvalidOperationException("La planification est inactive.");

            if (planif.Plages == null || planif.Plages.Count == 0)
                throw new InvalidOperationException("La planification n'a aucune plage horaire.");

            var (debut, fin) = RestaurantPlanificationDateHelper.ResolvePeriode(
                request.Mode, request.DateDebut, request.DateFin);

            var candidateDates = RestaurantPlanificationDateHelper.ExpandDates(
                debut, fin, planif.JoursSemaine);

            var details = new List<RestaurantPlanificationGenerationDetailDto>();
            var plages = planif.Plages
                .OrderBy(p => p.Ordre)
                .ThenBy(p => p.StartTime)
                .ToList();

            foreach (var dateService in candidateDates)
            {
                foreach (var plage in plages)
                {
                    var startAtUtc = RestaurantPlanificationTimeHelper.ToUtc(dateService, plage.StartTime);
                    var endAtUtc = RestaurantPlanificationTimeHelper.ToUtc(dateService, plage.EndTime);

                    var exists = await _context.RestaurantCreneaux.AsNoTracking()
                        .AnyAsync(
                            c => c.IdRestaurant == planif.IdRestaurant
                                 && c.DateService == dateService
                                 && c.StartAtUtc == startAtUtc,
                            cancellationToken);

                    if (exists)
                    {
                        details.Add(new RestaurantPlanificationGenerationDetailDto
                        {
                            DateService = dateService,
                            StartAtUtc = startAtUtc,
                            Statut = PlanificationGenerationItemStatut.Ignore,
                            Message = "Créneau déjà existant pour cette date et heure de début"
                        });
                        continue;
                    }

                    try
                    {
                        var createRequest = BuildCreateRequest(planif, plage, dateService, startAtUtc, endAtUtc);
                        var created = await _creneauService.CreateDraftAsync(
                            createRequest,
                            planif.IdSociete,
                            planif.IdRestaurantPlanification,
                            plage.IdRestaurantPlanificationPlage,
                            cancellationToken);

                        var detail = new RestaurantPlanificationGenerationDetailDto
                        {
                            DateService = dateService,
                            StartAtUtc = startAtUtc,
                            Statut = PlanificationGenerationItemStatut.Cree,
                            IdCreneau = created.IdRestaurantCreneau
                        };

                        if (request.PublierApresGeneration)
                        {
                            try
                            {
                                await _creneauService.PublishAsync(
                                    created.IdRestaurantCreneau,
                                    planif.IdSociete,
                                    cancellationToken);
                                detail.Publiee = true;
                            }
                            catch (Exception publishEx)
                            {
                                _logger.LogWarning(
                                    publishEx,
                                    "Publish après génération échoué — Planif={PlanifId}, Creneau={CreneauId}, Date={Date}",
                                    idPlanification,
                                    created.IdRestaurantCreneau,
                                    dateService);

                                detail.Publiee = false;
                                detail.Message =
                                    $"Créé en Draft ; publish échoué : {publishEx.Message}";
                            }
                        }

                        details.Add(detail);
                    }
                    catch (RestaurantCreneauConflictException ex)
                    {
                        details.Add(new RestaurantPlanificationGenerationDetailDto
                        {
                            DateService = dateService,
                            StartAtUtc = startAtUtc,
                            Statut = PlanificationGenerationItemStatut.Ignore,
                            Message = ex.Message
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Échec génération créneau planif {PlanifId} date {Date} start {Start}",
                            idPlanification,
                            dateService,
                            startAtUtc);

                        details.Add(new RestaurantPlanificationGenerationDetailDto
                        {
                            DateService = dateService,
                            StartAtUtc = startAtUtc,
                            Statut = PlanificationGenerationItemStatut.Echec,
                            Message = ex.Message
                        });
                    }
                }
            }

            var resume = new RestaurantPlanificationGenerationResumeDto
            {
                Creees = details.Count(d => d.Statut == PlanificationGenerationItemStatut.Cree),
                Ignorees = details.Count(d => d.Statut == PlanificationGenerationItemStatut.Ignore),
                Echecs = details.Count(d => d.Statut == PlanificationGenerationItemStatut.Echec),
                Publiees = details.Count(d => d.Publiee)
            };

            var log = new RestaurantPlanifGenerationLog
            {
                IdRestaurantPlanification = idPlanification,
                DateDebut = debut,
                DateFin = fin,
                NombreCrees = resume.Creees,
                NombreIgnores = resume.Ignorees,
                NombreEchecs = resume.Echecs,
                NombrePublies = resume.Publiees,
                DetailsJson = JsonSerializer.Serialize(details),
                DeclencheParIdUtilisateur = declencheParIdUtilisateur,
                DateCreation = DateTime.UtcNow
            };

            _context.RestaurantPlanifGenerationLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Génération planification restaurant {PlanifId}: {Crees} créés, {Publiees} publiés, {Ignores} ignorés, {Echecs} échecs",
                idPlanification, resume.Creees, resume.Publiees, resume.Ignorees, resume.Echecs);

            return new RestaurantPlanificationGenerationResultDto
            {
                IdGeneration = log.IdRestaurantPlanifGenerationLog,
                Planification = new RestaurantPlanificationGenerationPlanifSummaryDto
                {
                    Id = planif.IdRestaurantPlanification,
                    Libelle = planif.Libelle
                },
                Periode = new RestaurantPlanificationGenerationPeriodeDto
                {
                    DateDebut = debut,
                    DateFin = fin
                },
                Resume = resume,
                Details = details
            };
        }

        private static RestaurantCreateCreneauRequestDto BuildCreateRequest(
            RestaurantPlanification planif,
            RestaurantPlanificationPlage plage,
            DateOnly dateService,
            DateTime startAtUtc,
            DateTime endAtUtc)
        {
            var request = new RestaurantCreateCreneauRequestDto
            {
                IdRestaurant = planif.IdRestaurant,
                DateService = dateService,
                StartAtUtc = startAtUtc,
                EndAtUtc = endAtUtc,
                InventoryMode = planif.InventoryMode.ToString(),
                CodeDevise = planif.CodeDevise,
                MontantAcompte = planif.MontantAcompte
            };

            if (planif.InventoryMode == RestaurantInventoryMode.GlobalQuota && plage.GlobalQuota != null)
            {
                request.GlobalQuota = new RestaurantCreateCreneauGlobalQuotaDto
                {
                    CapaciteTotale = plage.GlobalQuota.CapaciteTotale,
                    PrixUnitaire = plage.GlobalQuota.PrixUnitaire
                };
            }
            else if (planif.InventoryMode == RestaurantInventoryMode.ClassQuota)
            {
                request.ZoneQuotas = (plage.ZoneQuotas ?? Array.Empty<RestaurantPlanifPlageZoneQuota>())
                    .Select(q => new RestaurantCreateCreneauZoneQuotaDto
                    {
                        IdRestaurantZone = q.IdRestaurantZone,
                        CapaciteTotale = q.CapaciteTotale,
                        PrixUnitaire = q.PrixUnitaire
                    })
                    .ToList();
            }

            return request;
        }
    }
}
