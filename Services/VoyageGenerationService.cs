using System.Text.Json;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.PlanificationVoyage;
using CongoTravel.Models.DTOs.VoyageTarification;
using CongoTravel.Models.Enums;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class VoyageGenerationService : IVoyageGenerationService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IVoyageRepository _voyageRepository;
        private readonly IVoyageTarifService _voyageTarifService;
        private readonly ILogger<VoyageGenerationService> _logger;

        public VoyageGenerationService(
            CongoTravelDbContext context,
            IVoyageRepository voyageRepository,
            IVoyageTarifService voyageTarifService,
            ILogger<VoyageGenerationService> logger)
        {
            _context = context;
            _voyageRepository = voyageRepository;
            _voyageTarifService = voyageTarifService;
            _logger = logger;
        }

        public async Task<PlanificationGenerationResultDto> GenererAsync(
            int idPlanificationVoyage,
            GenererPlanificationVoyageDto request,
            int? declencheParIdUtilisateur = null,
            CancellationToken cancellationToken = default)
        {
            var planif = await _context.PlanificationsVoyage.AsNoTracking()
                .Include(p => p.Etapes)
                .Include(p => p.Tarifs)
                .FirstOrDefaultAsync(p => p.IdPlanificationVoyage == idPlanificationVoyage, cancellationToken);

            if (planif == null)
                throw new KeyNotFoundException($"Planification {idPlanificationVoyage} introuvable.");

            if (!planif.Statut)
                throw new InvalidOperationException("La planification est inactive.");

            var (debut, fin) = PlanificationVoyageDateHelper.ResolvePeriode(
                request.Mode, request.DateDebut, request.DateFin);

            var candidateDates = PlanificationVoyageDateHelper.ExpandDates(debut, fin, planif.JoursSemaine);
            var etapesVoyage = (planif.Etapes ?? Array.Empty<PlanificationVoyageEtape>())
                .OrderBy(e => e.Ordre)
                .Select(e => new CreateVoyageEtapeDto { Ordre = e.Ordre, IdDestination = e.IdDestination })
                .ToList();

            var tarifs = (planif.Tarifs ?? Array.Empty<PlanificationVoyageTarif>())
                .Select(t => (t.IdCategorieSiege, t.Prix))
                .ToList();

            var config = await _context.ConfigSocietes.AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdSociete == planif.IdSociete, cancellationToken);

            var horizonJours = config?.JoursAvanceMaxReservation ?? ConfigSocieteDefaults.JoursAvanceMaxReservationDefault;
            var maxReservableDate = DateTime.UtcNow.Date.AddDays(horizonJours);

            var details = new List<PlanificationGenerationDetailDto>();
            var avertissements = new List<string>();
            var horsHorizon = 0;

            foreach (var date in candidateDates)
            {
                if (date.Date > maxReservableDate)
                    horsHorizon++;

                var voyage = new Voyage
                {
                    DateDepart = date,
                    HeureDepart = planif.HeureDepart,
                    Prix = planif.Prix,
                    CodeDevisePrix = planif.CodeDevisePrix,
                    IdVehicule = planif.IdVehicule,
                    IdDestination = etapesVoyage[0].IdDestination,
                    IdSociete = planif.IdSociete,
                    IdSite = planif.IdSite,
                    Statut = true
                };

                var result = await _voyageRepository.TryCreateAsync(
                    voyage,
                    etapesVoyage,
                    new VoyageCreateOptions
                    {
                        IdPlanificationVoyage = idPlanificationVoyage,
                        ThrowOnConflict = false
                    });

                switch (result.Outcome)
                {
                    case VoyageCreateOutcome.Created:
                        if (tarifs.Count > 0)
                        {
                            await _voyageTarifService.ReplaceTarifsForVoyageAsync(
                                result.Voyage!.Id,
                                planif.IdSociete,
                                tarifs,
                                cancellationToken);
                        }

                        details.Add(new PlanificationGenerationDetailDto
                        {
                            DateDepart = date,
                            Statut = PlanificationGenerationItemStatut.Cree,
                            IdVoyage = result.Voyage!.Id
                        });
                        break;

                    case VoyageCreateOutcome.SkippedConflict:
                        details.Add(new PlanificationGenerationDetailDto
                        {
                            DateDepart = date,
                            Statut = PlanificationGenerationItemStatut.Ignore,
                            Message = result.Message ?? "Créneau déjà occupé"
                        });
                        break;

                    default:
                        details.Add(new PlanificationGenerationDetailDto
                        {
                            DateDepart = date,
                            Statut = PlanificationGenerationItemStatut.Echec,
                            Message = result.Message ?? "Erreur inconnue"
                        });
                        break;
                }
            }

            if (horsHorizon > 0)
            {
                avertissements.Add(
                    $"{horsHorizon} voyage(s) dépassent l'horizon de réservation ({horizonJours} jours) — créés mais non réservables immédiatement.");
            }

            var resume = new PlanificationGenerationResumeDto
            {
                Creees = details.Count(d => d.Statut == PlanificationGenerationItemStatut.Cree),
                Ignorees = details.Count(d => d.Statut == PlanificationGenerationItemStatut.Ignore),
                Echecs = details.Count(d => d.Statut == PlanificationGenerationItemStatut.Echec)
            };

            var log = new PlanificationGenerationLog
            {
                IdPlanificationVoyage = idPlanificationVoyage,
                DateDebut = debut,
                DateFin = fin,
                NombreCrees = resume.Creees,
                NombreIgnores = resume.Ignorees,
                NombreEchecs = resume.Echecs,
                DetailsJson = JsonSerializer.Serialize(details),
                DeclencheParIdUtilisateur = declencheParIdUtilisateur,
                DateCreation = DateTime.UtcNow
            };

            _context.PlanificationGenerationLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Génération planification {PlanifId}: {Crees} créés, {Ignores} ignorés, {Echecs} échecs",
                idPlanificationVoyage, resume.Creees, resume.Ignorees, resume.Echecs);

            return new PlanificationGenerationResultDto
            {
                IdGeneration = log.IdPlanificationGenerationLog,
                Planification = new PlanificationGenerationPlanifSummaryDto
                {
                    Id = planif.IdPlanificationVoyage,
                    Libelle = planif.Libelle
                },
                Periode = new PlanificationGenerationPeriodeDto { DateDebut = debut, DateFin = fin },
                Resume = resume,
                Avertissements = avertissements,
                Details = details
            };
        }
    }
}
