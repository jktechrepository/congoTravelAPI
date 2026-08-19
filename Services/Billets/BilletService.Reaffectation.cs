using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace CongoTravel.Services
{
    public partial class BilletService
    {
        public async Task<BilletReaffectationResult> ReaffecterBilletAsync(
            int idSociete,
            int idBillet,
            int idVoyageCible,
            int? idUtilisateurEnregistrement,
            bool confirmerPaiementDifferentiel = false,
            string? methodePaiement = null,
            string? referenceTransaction = null,
            string? commentaire = null)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();

                var billet = await GetBilletForOperationalLookupByIdAsync(idBillet);
                if (billet == null)
                {
                    return new BilletReaffectationResult
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = $"Billet {idBillet} introuvable."
                    };
                }

                if (billet.IdSociete != idSociete)
                {
                    return new BilletReaffectationResult
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "Le billet n'appartient pas à cette société."
                    };
                }

                var elig = await EvaluerEligibiliteReaffectationAsync(billet, idVoyageCible);
                if (!elig.Autorise)
                {
                    return new BilletReaffectationResult
                    {
                        Success = false,
                        StatusCode = elig.HttpStatus,
                        Message = elig.Message
                    };
                }

                var reservation = billet.Reservation!;
                if (!billet.IdReservationPassenger.HasValue)
                {
                    return new BilletReaffectationResult
                    {
                        Success = false,
                        StatusCode = 409,
                        Message = "Réaffectation impossible : le billet n'est pas associé à un passager (attribution de siège requise)."
                    };
                }

                var ancienVoyage = await _context.Voyages.FirstOrDefaultAsync(v => v.Id == reservation.IdVoyage);
                var nouveauVoyage = await _context.Voyages
                    .Include(v => v.Vehicule)
                    .FirstOrDefaultAsync(v => v.Id == idVoyageCible);

                if (ancienVoyage == null || nouveauVoyage == null)
                {
                    return new BilletReaffectationResult
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "Voyage source ou voyage cible introuvable."
                    };
                }

                var config = await _configSocieteRepository.GetOrCreateAsync(idSociete);
                if (!config.ReaffectationActive)
                {
                    return new BilletReaffectationResult
                    {
                        Success = false,
                        StatusCode = 409,
                        Message = "La réaffectation est désactivée pour cette société."
                    };
                }

                var now = DateTime.Now;
                var departSource = ancienVoyage.DateDepart.Date.Add(ancienVoyage.HeureDepart);
                var heuresLimiteReaffectation = Math.Clamp(config.HeuresLimiteReaffectation, 0, 72);
                var deadlineReaffectation = departSource.AddHours(-heuresLimiteReaffectation);
                if (now > deadlineReaffectation)
                {
                    _context.AuditLogs.Add(new AuditLog
                    {
                        TableName = "Billet",
                        RecordId = billet.IdBillet,
                        Action = "UPDATE",
                        UserId = idUtilisateurEnregistrement ?? 0,
                        UserName = $"user:{idUtilisateurEnregistrement?.ToString() ?? "system"}",
                        IdSociete = idSociete,
                        ChangedFields = "IdVoyage",
                        DateAction = now,
                        Success = false,
                        Commentaire = $"Réaffectation refusée (hors fenêtre). DepartSource={departSource:dd/MM/yyyy HH:mm}, Deadline={deadlineReaffectation:dd/MM/yyyy HH:mm}, LimiteHeures={heuresLimiteReaffectation}."
                    });
                    await _context.SaveChangesAsync();
                    return new BilletReaffectationResult
                    {
                        Success = false,
                        StatusCode = 409,
                        Message = "Réaffectation non autorisée: la fenêtre limite est dépassée.",
                        IdAncienVoyage = ancienVoyage.Id,
                        IdNouveauVoyage = nouveauVoyage.Id,
                        HeuresLimiteReaffectation = heuresLimiteReaffectation,
                        DepartVoyageSource = departSource,
                        DeadlineReaffectation = deadlineReaffectation
                    };
                }

                var differentiel = (decimal)(nouveauVoyage.Prix - ancienVoyage.Prix);
                var differentielPositif = Math.Max(differentiel, 0m);
                var departInitial = departSource;

                decimal penaliteTheorique;
                decimal? penalitePourcentageApplique = null;
                var montantPayeReference = 0m;

                if (billet.PenaliteOverride.HasValue)
                {
                    penaliteTheorique = Math.Max(0m, billet.PenaliteOverride.Value);
                }
                else
                {
                    montantPayeReference = await BilletMontantPayeHelper.ResolveMontantPayeBilletAsync(
                        _context,
                        _voyageTarifService,
                        billet,
                        ancienVoyage);
                    penalitePourcentageApplique = config.PenaliteReaffectationPourcentage;
                    penaliteTheorique = Math.Round(
                        montantPayeReference * config.PenaliteReaffectationPourcentage / 100m,
                        2,
                        MidpointRounding.AwayFromZero);
                }

                var penaliteAppliquee = now > departInitial ? penaliteTheorique : 0m;
                var montantRegularisation = differentielPositif + penaliteAppliquee;
                var paiementDifferentielRequis = montantRegularisation > 0m;
                if (paiementDifferentielRequis && !confirmerPaiementDifferentiel)
                {
                    return new BilletReaffectationResult
                    {
                        Success = false,
                        StatusCode = 409,
                        Message = $"Une régularisation de {montantRegularisation:0.##} est requise avant réaffectation (delta={differentielPositif:0.##}, penalite={penaliteAppliquee:0.##}).",
                        DifferentielTarifaire = differentiel,
                        Penalite = penaliteAppliquee,
                        PenaliteAppliquee = penaliteAppliquee > 0m,
                        PenalitePourcentageApplique = penalitePourcentageApplique,
                        MontantPayeReference = montantPayeReference,
                        MontantTotalRegularisation = montantRegularisation,
                        PaiementDifferentielRequis = true,
                        PaiementDifferentielConfirme = false,
                        IdAncienVoyage = ancienVoyage.Id,
                        IdNouveauVoyage = nouveauVoyage.Id,
                        HeuresLimiteReaffectation = heuresLimiteReaffectation,
                        DepartVoyageSource = departSource,
                        DeadlineReaffectation = deadlineReaffectation
                    };
                }

                if (paiementDifferentielRequis && confirmerPaiementDifferentiel)
                {
                    _context.Paiements.Add(new Paiement
                    {
                        MontantAPaye = montantRegularisation,
                        MontantPaye = montantRegularisation,
                        ResteAPaye = 0m,
                        MontantAPayeDevisePrincipale = montantRegularisation,
                        MontantPayeDevisePrincipale = montantRegularisation,
                        ResteAPayeDevisePrincipale = 0m,
                        CodeDevisePaiement = ancienVoyage.CodeDevisePrix,
                        CodeDevisePrincipale = ancienVoyage.CodeDevisePrincipale,
                        TauxVersDevisePrincipale = ancienVoyage.TauxVersDevisePrincipale <= 0m ? 1m : ancienVoyage.TauxVersDevisePrincipale,
                        DatePaiement = DateTime.UtcNow,
                        MethodePaiement = string.IsNullOrWhiteSpace(methodePaiement) ? "REGULARISATION_REAFFECTATION" : methodePaiement.Trim(),
                        ReferenceTransaction = string.IsNullOrWhiteSpace(referenceTransaction) ? null : referenceTransaction.Trim(),
                        Statut = true,
                        IdUtilisateur = idUtilisateurEnregistrement ?? reservation.IdUtilisateur,
                        IdReservation = reservation.IdReservation,
                        IdSociete = idSociete,
                        IdSite = reservation.IdSite,
                        DateCreation = DateTime.UtcNow
                    });
                }

                var okSiege = await TryMoveSeatAllocationAsync(
                    billet.IdReservationPassenger.Value,
                    ancienVoyage.Id,
                    nouveauVoyage,
                    billet);
                if (!okSiege)
                {
                    return new BilletReaffectationResult
                    {
                        Success = false,
                        StatusCode = 409,
                        Message = "Aucun siège disponible dans la catégorie du billet pour le voyage cible (réservation ou paiement en cours).",
                            DifferentielTarifaire = differentiel,
                            Penalite = penaliteAppliquee,
                            PenaliteAppliquee = penaliteAppliquee > 0m,
                            PenalitePourcentageApplique = penalitePourcentageApplique,
                            MontantPayeReference = montantPayeReference,
                            MontantTotalRegularisation = montantRegularisation,
                            PaiementDifferentielRequis = paiementDifferentielRequis,
                            PaiementDifferentielConfirme = confirmerPaiementDifferentiel,
                            HeuresLimiteReaffectation = heuresLimiteReaffectation,
                            DepartVoyageSource = departSource,
                            DeadlineReaffectation = deadlineReaffectation
                        };
                }

                reservation.IdVoyage = nouveauVoyage.Id;
                reservation.DateModification = DateTime.Now;
                billet.DateModification = DateTime.Now;

                _context.AuditLogs.Add(new AuditLog
                {
                    TableName = "Billet",
                    RecordId = billet.IdBillet,
                    Action = "UPDATE",
                    UserId = idUtilisateurEnregistrement ?? 0,
                    UserName = $"user:{idUtilisateurEnregistrement?.ToString() ?? "system"}",
                    IdSociete = idSociete,
                    ChangedFields = "IdVoyage,DifferentielTarifaire,Penalite,MontantRegularisation",
                    DateAction = DateTime.Now,
                    Success = true,
                    Commentaire = $"Réaffectation billet vers voyage {nouveauVoyage.Id}. Delta={differentielPositif:0.##}, Penalite={penaliteAppliquee:0.##}, Total={montantRegularisation:0.##}. {commentaire}".Trim()
                });

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                var billetComplet = await GetByIdAsync(idBillet);
                return new BilletReaffectationResult
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "Billet réaffecté avec succès.",
                    DifferentielTarifaire = differentiel,
                    Penalite = penaliteAppliquee,
                    PenaliteAppliquee = penaliteAppliquee > 0m,
                    PenalitePourcentageApplique = penalitePourcentageApplique,
                    MontantPayeReference = montantPayeReference,
                    MontantTotalRegularisation = montantRegularisation,
                    PaiementDifferentielRequis = paiementDifferentielRequis,
                    PaiementDifferentielConfirme = confirmerPaiementDifferentiel,
                    IdAncienVoyage = ancienVoyage.Id,
                    IdNouveauVoyage = nouveauVoyage.Id,
                    HeuresLimiteReaffectation = heuresLimiteReaffectation,
                    DepartVoyageSource = departSource,
                    DeadlineReaffectation = deadlineReaffectation,
                    Billet = billetComplet
                };
            });
        }

        private async Task<bool> TryMoveSeatAllocationAsync(
            int idReservationPassenger,
            int idVoyageSource,
            Voyage voyageCible,
            Billet billet)
        {
            var allocSource = await _context.VoyageSeatAllocations
                .Include(a => a.Siege)
                .FirstOrDefaultAsync(a =>
                    a.IdReservationPassenger == idReservationPassenger
                    && a.IdVoyage == idVoyageSource
                    && a.Statut == "CONFIRME");
            if (allocSource == null)
                return false;

            var categorie = allocSource.Siege?.IdCategorieSiege;
            if (!categorie.HasValue || voyageCible.Vehicule == null)
                return false;

            var indisponibles = await _siegeDisponibiliteService.GetIndisponibleSiegeIdsAsync(voyageCible.Id);

            var siegeCible = await _context.Sieges
                .Where(s =>
                    s.IdVehicule == voyageCible.IdVehicule
                    && s.EstActif
                    && s.IdCategorieSiege == categorie.Value
                    && s.NumeroOrdre <= voyageCible.Vehicule.NombreSiege
                    && !indisponibles.Contains(s.IdSiege))
                .OrderBy(s => s.NumeroOrdre)
                .FirstOrDefaultAsync();
            if (siegeCible == null)
                return false;

            allocSource.IdVoyage = voyageCible.Id;
            allocSource.IdSiege = siegeCible.IdSiege;
            allocSource.DateModification = DateTime.UtcNow;

            billet.IdSiege = siegeCible.IdSiege;
            billet.CodeSiege = siegeCible.CodeSiege;
            return true;
        }
    }
}
