using System.Text.Json;
using CongoTravel.Data;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Enums;
using CongoTravel.Models.Hotel;
using CongoTravel.Models.Hotel.Enums;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.Hotel
{
    public class HotelAllotmentGenerationService : IHotelAllotmentGenerationService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IHotelAllotmentService _allotmentService;
        private readonly IHotelNightService _nightService;
        private readonly ILogger<HotelAllotmentGenerationService> _logger;

        public HotelAllotmentGenerationService(
            CongoTravelDbContext context,
            IHotelAllotmentService allotmentService,
            IHotelNightService nightService,
            ILogger<HotelAllotmentGenerationService> logger)
        {
            _context = context;
            _allotmentService = allotmentService;
            _nightService = nightService;
            _logger = logger;
        }

        public async Task<HotelPlanificationGenerationResultDto> GenererAsync(
            int idPlanification,
            GenererHotelPlanificationDto request,
            int? declencheParIdUtilisateur = null,
            CancellationToken cancellationToken = default)
        {
            var planif = await _context.HotelPlanifications.AsNoTracking()
                .Include(p => p.Lignes)
                .Include(p => p.GlobalQuota)
                .FirstOrDefaultAsync(p => p.IdHotelPlanification == idPlanification, cancellationToken);

            if (planif == null)
                throw new KeyNotFoundException($"Planification {idPlanification} introuvable.");

            if (!planif.Statut)
                throw new InvalidOperationException("La planification est inactive.");

            if (planif.InventoryMode == HotelInventoryMode.ClassQuota
                && (planif.Lignes == null || planif.Lignes.Count == 0))
                throw new InvalidOperationException("La planification n'a aucune ligne (type de chambre).");

            if (planif.InventoryMode == HotelInventoryMode.GlobalQuota && planif.GlobalQuota == null)
                throw new InvalidOperationException("La planification GlobalQuota n'a pas de quota global.");

            var (debut, fin) = HotelPlanificationDateHelper.ResolvePeriode(
                request.Mode, request.DateDebut, request.DateFin);

            var candidateDates = HotelPlanificationDateHelper.ExpandDates(
                debut, fin, planif.JoursSemaine);

            var details = planif.InventoryMode == HotelInventoryMode.GlobalQuota
                ? await GenerateGlobalNightsAsync(planif, idPlanification, candidateDates, request, cancellationToken)
                : await GenerateClassAllotmentsAsync(planif, idPlanification, candidateDates, request, cancellationToken);

            var resume = new HotelPlanificationGenerationResumeDto
            {
                Creees = details.Count(d => d.Statut == PlanificationGenerationItemStatut.Cree),
                Ignorees = details.Count(d => d.Statut == PlanificationGenerationItemStatut.Ignore),
                Echecs = details.Count(d => d.Statut == PlanificationGenerationItemStatut.Echec),
                Publiees = details.Count(d => d.Publiee)
            };

            var log = new HotelPlanifGenerationLog
            {
                IdHotelPlanification = idPlanification,
                DateDebut = debut,
                DateFin = fin,
                NombreCrees = resume.Creees,
                NombreIgnores = resume.Ignorees,
                NombreEchecs = resume.Echecs,
                DetailsJson = JsonSerializer.Serialize(details),
                DeclencheParIdUtilisateur = declencheParIdUtilisateur,
                DateCreation = DateTime.UtcNow
            };

            _context.HotelPlanifGenerationLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Génération planification hôtel {PlanifId} ({Mode}): {Crees} créés, {Publiees} publiés, {Ignores} ignorés, {Echecs} échecs",
                idPlanification, planif.InventoryMode, resume.Creees, resume.Publiees, resume.Ignorees, resume.Echecs);

            return new HotelPlanificationGenerationResultDto
            {
                IdGeneration = log.IdHotelPlanifGenerationLog,
                Planification = new HotelPlanificationGenerationPlanifSummaryDto
                {
                    Id = planif.IdHotelPlanification,
                    Libelle = planif.Libelle
                },
                Periode = new HotelPlanificationGenerationPeriodeDto
                {
                    DateDebut = debut,
                    DateFin = fin
                },
                Resume = resume,
                Details = details
            };
        }

        private async Task<List<HotelPlanificationGenerationDetailDto>> GenerateClassAllotmentsAsync(
            HotelPlanification planif,
            int idPlanification,
            IReadOnlyList<DateOnly> candidateDates,
            GenererHotelPlanificationDto request,
            CancellationToken cancellationToken)
        {
            var details = new List<HotelPlanificationGenerationDetailDto>();

            foreach (var nightDate in candidateDates)
            {
                var nightDateTime = nightDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
                foreach (var ligne in planif.Lignes!)
                {
                    var exists = await _context.HotelNightAllotments.AsNoTracking()
                        .AnyAsync(
                            a => a.IdHotel == planif.IdHotel
                                 && a.IdHotelRoomType == ligne.IdHotelRoomType
                                 && a.NightDate == nightDateTime,
                            cancellationToken);

                    if (exists)
                    {
                        details.Add(new HotelPlanificationGenerationDetailDto
                        {
                            NightDate = nightDate,
                            IdHotelRoomType = ligne.IdHotelRoomType,
                            Statut = PlanificationGenerationItemStatut.Ignore,
                            Message = "Allotment déjà existant pour cette nuit × type"
                        });
                        continue;
                    }

                    try
                    {
                        var createRequest = new HotelCreateAllotmentRequestDto
                        {
                            IdHotel = planif.IdHotel,
                            IdHotelRoomType = ligne.IdHotelRoomType,
                            NightDate = nightDateTime,
                            CapaciteTotale = ligne.CapaciteTotale,
                            PrixNuit = ligne.PrixNuit,
                            CodeDevise = planif.CodeDevise,
                            IdHotelPlanification = idPlanification
                        };

                        var created = await _allotmentService.CreateDraftAsync(
                            createRequest,
                            planif.IdSociete,
                            cancellationToken);

                        var detail = new HotelPlanificationGenerationDetailDto
                        {
                            NightDate = nightDate,
                            IdHotelRoomType = ligne.IdHotelRoomType,
                            Statut = PlanificationGenerationItemStatut.Cree,
                            IdHotelNightAllotment = created.IdHotelNightAllotment
                        };

                        if (request.PublierApresGeneration)
                        {
                            try
                            {
                                await _allotmentService.PublishAsync(
                                    created.IdHotelNightAllotment,
                                    planif.IdSociete,
                                    cancellationToken);
                                detail.Publiee = true;
                            }
                            catch (Exception publishEx)
                            {
                                _logger.LogWarning(
                                    publishEx,
                                    "Publish après génération échoué — Planif={PlanifId}, Allotment={AllotmentId}, Night={Night}",
                                    idPlanification,
                                    created.IdHotelNightAllotment,
                                    nightDate);

                                detail.Publiee = false;
                                detail.Message =
                                    $"Créé en Draft ; publish échoué : {publishEx.Message}";
                            }
                        }

                        details.Add(detail);
                    }
                    catch (HotelNightAllotmentConflictException ex)
                    {
                        details.Add(new HotelPlanificationGenerationDetailDto
                        {
                            NightDate = nightDate,
                            IdHotelRoomType = ligne.IdHotelRoomType,
                            Statut = PlanificationGenerationItemStatut.Ignore,
                            Message = ex.Message
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Échec génération allotment planif {PlanifId} night {Night} type {Type}",
                            idPlanification,
                            nightDate,
                            ligne.IdHotelRoomType);

                        details.Add(new HotelPlanificationGenerationDetailDto
                        {
                            NightDate = nightDate,
                            IdHotelRoomType = ligne.IdHotelRoomType,
                            Statut = PlanificationGenerationItemStatut.Echec,
                            Message = ex.Message
                        });
                    }
                }
            }

            return details;
        }

        private async Task<List<HotelPlanificationGenerationDetailDto>> GenerateGlobalNightsAsync(
            HotelPlanification planif,
            int idPlanification,
            IReadOnlyList<DateOnly> candidateDates,
            GenererHotelPlanificationDto request,
            CancellationToken cancellationToken)
        {
            var details = new List<HotelPlanificationGenerationDetailDto>();
            var quota = planif.GlobalQuota!;

            foreach (var nightDate in candidateDates)
            {
                var nightDateTime = nightDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

                var exists = await _context.HotelNights.AsNoTracking()
                    .AnyAsync(
                        n => n.IdHotel == planif.IdHotel && n.NightDate == nightDateTime,
                        cancellationToken);

                if (exists)
                {
                    details.Add(new HotelPlanificationGenerationDetailDto
                    {
                        NightDate = nightDate,
                        IdHotelRoomType = 0,
                        Statut = PlanificationGenerationItemStatut.Ignore,
                        Message = "Nuit GlobalQuota déjà existante pour cette date"
                    });
                    continue;
                }

                try
                {
                    var createRequest = new HotelCreateNightRequestDto
                    {
                        IdHotel = planif.IdHotel,
                        NightDate = nightDateTime,
                        CapaciteTotale = quota.CapaciteTotale,
                        PrixNuit = quota.PrixNuit,
                        CodeDevise = planif.CodeDevise,
                        IdHotelPlanification = idPlanification
                    };

                    var created = await _nightService.CreateDraftAsync(
                        createRequest,
                        planif.IdSociete,
                        cancellationToken);

                    var detail = new HotelPlanificationGenerationDetailDto
                    {
                        NightDate = nightDate,
                        IdHotelRoomType = 0,
                        Statut = PlanificationGenerationItemStatut.Cree,
                        IdHotelNight = created.IdHotelNight
                    };

                    if (request.PublierApresGeneration)
                    {
                        try
                        {
                            await _nightService.PublishAsync(
                                created.IdHotelNight,
                                planif.IdSociete,
                                cancellationToken);
                            detail.Publiee = true;
                        }
                        catch (Exception publishEx)
                        {
                            _logger.LogWarning(
                                publishEx,
                                "Publish après génération échoué — Planif={PlanifId}, Night={NightId}, Date={Night}",
                                idPlanification,
                                created.IdHotelNight,
                                nightDate);

                            detail.Publiee = false;
                            detail.Message =
                                $"Créée en Draft ; publish échoué : {publishEx.Message}";
                        }
                    }

                    details.Add(detail);
                }
                catch (HotelNightConflictException ex)
                {
                    details.Add(new HotelPlanificationGenerationDetailDto
                    {
                        NightDate = nightDate,
                        IdHotelRoomType = 0,
                        Statut = PlanificationGenerationItemStatut.Ignore,
                        Message = ex.Message
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Échec génération nuit GlobalQuota planif {PlanifId} night {Night}",
                        idPlanification,
                        nightDate);

                    details.Add(new HotelPlanificationGenerationDetailDto
                    {
                        NightDate = nightDate,
                        IdHotelRoomType = 0,
                        Statut = PlanificationGenerationItemStatut.Echec,
                        Message = ex.Message
                    });
                }
            }

            return details;
        }
    }
}
