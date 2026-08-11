using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.Enums;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique
{
    public class SiteTouristiqueJourneeGenerationService : ISiteTouristiqueJourneeGenerationService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ISiteTouristiqueJourneeService _journeeService;
        private readonly ILogger<SiteTouristiqueJourneeGenerationService> _logger;

        public SiteTouristiqueJourneeGenerationService(
            CongoTravelDbContext context,
            ISiteTouristiqueJourneeService journeeService,
            ILogger<SiteTouristiqueJourneeGenerationService> logger)
        {
            _context = context;
            _journeeService = journeeService;
            _logger = logger;
        }

        public async Task<SiteTouristiquePlanificationGenerationResultDto> GenererAsync(
            int idPlanification,
            GenererSiteTouristiquePlanificationDto request,
            int? declencheParIdUtilisateur = null,
            CancellationToken cancellationToken = default)
        {
            var planif = await _context.SiteTouristiquePlanifications.AsNoTracking()
                .Include(p => p.GlobalQuota)
                .Include(p => p.ClassQuotas)
                .FirstOrDefaultAsync(p => p.IdSiteTouristiquePlanification == idPlanification, cancellationToken);

            if (planif == null)
                throw new KeyNotFoundException($"Planification {idPlanification} introuvable.");

            if (!planif.Statut)
                throw new InvalidOperationException("La planification est inactive.");

            var (debut, fin) = SiteTouristiquePlanificationDateHelper.ResolvePeriode(
                request.Mode, request.DateDebut, request.DateFin);

            var candidateDates = SiteTouristiquePlanificationDateHelper.ExpandDates(
                debut, fin, planif.JoursSemaine);

            var details = new List<SiteTouristiquePlanificationGenerationDetailDto>();

            foreach (var dateVisite in candidateDates)
            {
                var exists = await _context.SiteTouristiqueJournees.AsNoTracking()
                    .AnyAsync(
                        j => j.IdSiteTouristique == planif.IdSiteTouristique
                             && j.DateVisite == dateVisite,
                        cancellationToken);

                if (exists)
                {
                    details.Add(new SiteTouristiquePlanificationGenerationDetailDto
                    {
                        DateVisite = dateVisite,
                        Statut = PlanificationGenerationItemStatut.Ignore,
                        Message = "Journée déjà existante pour cette date"
                    });
                    continue;
                }

                try
                {
                    var createRequest = BuildCreateRequest(planif, dateVisite);
                    var created = await _journeeService.CreateDraftAsync(
                        createRequest,
                        planif.IdSociete,
                        idPlanification,
                        cancellationToken);

                    var detail = new SiteTouristiquePlanificationGenerationDetailDto
                    {
                        DateVisite = dateVisite,
                        Statut = PlanificationGenerationItemStatut.Cree,
                        IdJournee = created.IdSiteTouristiqueJournee
                    };

                    if (request.PublierApresGeneration)
                    {
                        try
                        {
                            await _journeeService.PublishAsync(
                                created.IdSiteTouristiqueJournee,
                                planif.IdSociete,
                                cancellationToken);
                            detail.Publiee = true;
                        }
                        catch (Exception publishEx)
                        {
                            _logger.LogWarning(
                                publishEx,
                                "Publish après génération échoué — Planif={PlanifId}, Journee={JourneeId}, Date={Date}",
                                idPlanification,
                                created.IdSiteTouristiqueJournee,
                                dateVisite);

                            detail.Publiee = false;
                            detail.Message =
                                $"Créée en Draft ; publish échoué : {publishEx.Message}";
                        }
                    }

                    details.Add(detail);
                }
                catch (SiteTouristiqueJourneeConflictException ex)
                {
                    details.Add(new SiteTouristiquePlanificationGenerationDetailDto
                    {
                        DateVisite = dateVisite,
                        Statut = PlanificationGenerationItemStatut.Ignore,
                        Message = ex.Message
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Échec génération journée planif {PlanifId} date {Date}",
                        idPlanification,
                        dateVisite);

                    details.Add(new SiteTouristiquePlanificationGenerationDetailDto
                    {
                        DateVisite = dateVisite,
                        Statut = PlanificationGenerationItemStatut.Echec,
                        Message = ex.Message
                    });
                }
            }

            var resume = new SiteTouristiquePlanificationGenerationResumeDto
            {
                Creees = details.Count(d => d.Statut == PlanificationGenerationItemStatut.Cree),
                Ignorees = details.Count(d => d.Statut == PlanificationGenerationItemStatut.Ignore),
                Echecs = details.Count(d => d.Statut == PlanificationGenerationItemStatut.Echec),
                Publiees = details.Count(d => d.Publiee)
            };

            var log = new SiteTouristiquePlanifGenerationLog
            {
                IdSiteTouristiquePlanification = idPlanification,
                DateDebut = debut,
                DateFin = fin,
                NombreCrees = resume.Creees,
                NombreIgnores = resume.Ignorees,
                NombreEchecs = resume.Echecs,
                DetailsJson = JsonSerializer.Serialize(details),
                DeclencheParIdUtilisateur = declencheParIdUtilisateur,
                DateCreation = DateTime.UtcNow
            };

            _context.SiteTouristiquePlanifGenerationLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Génération planification ST {PlanifId}: {Crees} créés, {Publiees} publiés, {Ignores} ignorés, {Echecs} échecs",
                idPlanification, resume.Creees, resume.Publiees, resume.Ignorees, resume.Echecs);

            return new SiteTouristiquePlanificationGenerationResultDto
            {
                IdGeneration = log.IdSiteTouristiquePlanifGenerationLog,
                Planification = new SiteTouristiquePlanificationGenerationPlanifSummaryDto
                {
                    Id = planif.IdSiteTouristiquePlanification,
                    Libelle = planif.Libelle
                },
                Periode = new SiteTouristiquePlanificationGenerationPeriodeDto
                {
                    DateDebut = debut,
                    DateFin = fin
                },
                Resume = resume,
                Details = details
            };
        }

        private static SiteTouristiqueCreateJourneeRequestDto BuildCreateRequest(
            SiteTouristiquePlanification planif,
            DateOnly dateVisite)
        {
            var startOfDay = dateVisite.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            DateTime? salesOpen = planif.SalesOpenOffsetHours.HasValue
                ? startOfDay.AddHours(-planif.SalesOpenOffsetHours.Value)
                : null;
            DateTime? salesClose = planif.SalesCloseOffsetHours.HasValue
                ? startOfDay.AddHours(24).AddHours(-planif.SalesCloseOffsetHours.Value)
                : null;

            var request = new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = planif.IdSiteTouristique,
                DateVisite = dateVisite,
                InventoryMode = planif.InventoryMode.ToString(),
                CodeDevise = planif.CodeDevise,
                SalesOpenAtUtc = salesOpen,
                SalesCloseAtUtc = salesClose
            };

            if (planif.InventoryMode == SiteTouristiqueInventoryMode.GlobalQuota && planif.GlobalQuota != null)
            {
                request.GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = planif.GlobalQuota.CapaciteTotale,
                    PrixUnitaire = planif.GlobalQuota.PrixUnitaire
                };
            }
            else if (planif.InventoryMode == SiteTouristiqueInventoryMode.ClassQuota)
            {
                request.ClassQuotas = (planif.ClassQuotas ?? Array.Empty<SiteTouristiquePlanifClassQuota>())
                    .Select(q => new SiteTouristiqueCreateJourneeClassQuotaDto
                    {
                        IdSiteTouristiqueClasse = q.IdSiteTouristiqueClasse,
                        CapaciteTotale = q.CapaciteTotale,
                        PrixUnitaire = q.PrixUnitaire
                    })
                    .ToList();
            }

            return request;
        }
    }
}
