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
        private static DateTime HoraireDepartLocal(DateTime dateDepart, TimeSpan heureDepart) =>
            dateDepart.Date.Add(heureDepart);

        private static bool EstReservationConfirmee(string? statut)
        {
            if (string.IsNullOrWhiteSpace(statut)) return false;
            var s = statut.Trim();
            return s.Equals("CONFIRMEE", StringComparison.OrdinalIgnoreCase)
                || s.Equals("CONFIRME", StringComparison.OrdinalIgnoreCase);
        }

        private static bool EstReservationAnnulee(string? statut)
        {
            if (string.IsNullOrWhiteSpace(statut)) return false;
            var s = statut.Trim();
            return s.Equals("ANNULE", StringComparison.OrdinalIgnoreCase)
                || s.Equals("ANNULEE", StringComparison.OrdinalIgnoreCase)
                || s.StartsWith("ANNUL", StringComparison.OrdinalIgnoreCase);
        }

        private static bool EstReservationEnAttente(string? statut) =>
            statut?.Trim().Equals("EN_ATTENTE", StringComparison.OrdinalIgnoreCase) == true;

        /// <summary>Violation d’unicité sur <c>IX_BilletEmbarquements_IdBillet_Unique</c> (course entre deux scans).</summary>
        private static bool IsDuplicateBilletEmbarquementIndex(DbUpdateException ex)
        {
            var mySqlEx = ex.InnerException as MySqlException
                ?? ex.InnerException?.InnerException as MySqlException;
            if (mySqlEx == null)
                return false;
            if (mySqlEx.Number != 1062 && mySqlEx.ErrorCode != MySqlErrorCode.DuplicateKeyEntry)
                return false;

            var msg = mySqlEx.Message ?? string.Empty;
            return msg.Contains("IX_BilletEmbarquements_IdBillet", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("BilletEmbarquements", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Résultat interne partagé entre les contrôles de billet et <see cref="EnregistrerEmbarquementAsync"/>.</summary>
        private sealed class BilletEligibiliteResult
        {
            public bool Autorise { get; init; }
            public string Code { get; init; } = "";
            public string Message { get; init; } = "";
            public int HttpStatus { get; init; } = 400;
            public int? IdReservation { get; init; }
            public string? StatutReservation { get; init; }
            public DateTime? DateDepartVoyage { get; init; }
            public TimeSpan? HeureDepartVoyage { get; init; }
            public Voyage? VoyageReference { get; init; }
        }

        private static BilletCheckResponseDto ToCheckResponseDto(Billet billet, BilletEligibiliteResult e)
        {
            var voyage = e.VoyageReference ?? billet.Reservation?.Voyage;
            return new BilletCheckResponseDto
            {
                IdBillet = billet.IdBillet,
                IsUsed = billet.IsUsed,
                Statut = e.Code,
                Message = e.Message,
                EmbarquementAutorise = e.Autorise,
                IdReservation = e.IdReservation ?? billet.IdReservation,
                StatutReservation = e.StatutReservation ?? billet.Reservation?.StatutReservation,
                DateDepartVoyage = e.DateDepartVoyage ?? voyage?.DateDepart.Date,
                HeureDepartVoyage = e.HeureDepartVoyage ?? voyage?.HeureDepart,
                NomClient = billet.ReservationPassenger?.NomComplet,
                TelephoneClient = billet.ReservationPassenger?.Telephone
            };
        }

        /// <summary>
        /// Règles communes : usage, historique d’embarquement, réservation, voyage, fenêtre horaire (jour civil du départ, pas l’heure théorique du trajet).
        /// </summary>
        /// <param name="billet">Billet déjà chargé (id. <see cref="GetByIdAsync"/> avec includes).</param>
        private async Task<BilletEligibiliteResult> EvaluerEligibiliteEmbarquementAsync(Billet billet, int? idVoyageCible = null)
        {
            if (billet.IsUsed)
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "DejaUtilise",
                    HttpStatus = 409,
                    Message = "Ce billet a déjà été utilisé (embarquement enregistré).",
                    IdReservation = billet.IdReservation,
                    StatutReservation = billet.Reservation?.StatutReservation
                };
            }

            if (await _context.BilletEmbarquements.AnyAsync(e => e.IdBillet == billet.IdBillet))
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "EmbarquementDejaEnregistre",
                    HttpStatus = 409,
                    Message = "Un enregistrement d'embarquement existe déjà pour ce billet.",
                    IdReservation = billet.IdReservation,
                    StatutReservation = billet.Reservation?.StatutReservation
                };
            }

            if (!billet.IdReservation.HasValue)
            {
                return new BilletEligibiliteResult
                {
                    Autorise = true,
                    Code = "ValideSansReservation",
                    HttpStatus = 200,
                    Message =
                        "Billet reconnu et non utilisé. Aucune réservation n'est liée : les contrôles de confirmation de réservation et de fenêtre de voyage ne s'appliquent pas."
                };
            }

            var res = billet.Reservation;
            if (res == null)
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "ReservationInvalide",
                    HttpStatus = 400,
                    Message =
                        "La réservation associée à ce billet est introuvable. Le billet ne peut pas être utilisé pour l'embarquement.",
                    IdReservation = billet.IdReservation
                };
            }

            if (!res.Statut)
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "ReservationInactive",
                    HttpStatus = 400,
                    Message = "La réservation liée à ce billet est désactivée. Embarquement non autorisé.",
                    IdReservation = res.IdReservation,
                    StatutReservation = res.StatutReservation
                };
            }

            if (EstReservationAnnulee(res.StatutReservation))
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "ReservationInvalide",
                    HttpStatus = 400,
                    Message =
                        "La réservation a été annulée. Ce billet n'est plus valide pour l'embarquement.",
                    IdReservation = res.IdReservation,
                    StatutReservation = res.StatutReservation
                };
            }

            if (EstReservationEnAttente(res.StatutReservation) || !EstReservationConfirmee(res.StatutReservation))
            {
                var msg = EstReservationEnAttente(res.StatutReservation)
                    ? "La réservation n'est pas encore confirmée (paiement en attente ou incomplet). Le billet n'est pas utilisable pour l'embarquement."
                    : $"Le statut de réservation « {res.StatutReservation} » ne permet pas l'embarquement.";
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "ReservationInvalide",
                    HttpStatus = 400,
                    Message = msg,
                    IdReservation = res.IdReservation,
                    StatutReservation = res.StatutReservation
                };
            }

            var voyage = res.Voyage;
            if (voyage == null || voyage.Statut == false)
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "VoyageIndisponible",
                    HttpStatus = 400,
                    Message = voyage == null
                        ? "Le voyage associé à cette réservation est introuvable. Embarquement non autorisé."
                        : "Le voyage associé à cette réservation n'est plus disponible. Embarquement non autorisé.",
                    IdReservation = res.IdReservation,
                    StatutReservation = res.StatutReservation
                };
            }

            var voyageReference = voyage;
            if (idVoyageCible.HasValue)
            {
                var voyageCible = await _context.Voyages
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == idVoyageCible.Value);

                if (voyageCible == null || voyageCible.Statut == false)
                {
                    return new BilletEligibiliteResult
                    {
                        Autorise = false,
                        Code = "VoyageIndisponible",
                        HttpStatus = 400,
                        Message = "Le voyage cible est introuvable ou inactif.",
                        IdReservation = res.IdReservation,
                        StatutReservation = res.StatutReservation
                    };
                }

                if (voyageCible.IdSociete != billet.IdSociete)
                {
                    return new BilletEligibiliteResult
                    {
                        Autorise = false,
                        Code = "VoyageIncompatible",
                        HttpStatus = 400,
                        Message = "Le voyage cible n'appartient pas à la même société que le billet.",
                        IdReservation = res.IdReservation,
                        StatutReservation = res.StatutReservation
                    };
                }

                if (voyageCible.IdDestination != voyage.IdDestination)
                {
                    return new BilletEligibiliteResult
                    {
                        Autorise = false,
                        Code = "VoyageIncompatible",
                        HttpStatus = 400,
                        Message = "Le voyage cible n'a pas la même destination que le voyage d'origine du billet.",
                        IdReservation = res.IdReservation,
                        StatutReservation = res.StatutReservation
                    };
                }

                voyageReference = voyageCible;
            }

            var config = await _configSocieteRepository.GetOrCreateAsync(voyage.IdSociete);

            var now = DateTime.Now;
            var validite = ResolveBilletValidityWindow(billet, voyage, config.DureeValiditeBilletJours);
            if (validite.Start.HasValue && now < validite.Start.Value)
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "BilletPasEncoreValide",
                    HttpStatus = 400,
                    Message = $"Le billet n'est pas encore valide. Début de validité: {validite.Start:dd/MM/yyyy HH:mm}.",
                    IdReservation = res.IdReservation,
                    StatutReservation = res.StatutReservation,
                    DateDepartVoyage = voyageReference.DateDepart.Date,
                    HeureDepartVoyage = voyageReference.HeureDepart,
                    VoyageReference = voyageReference
                };
            }

            if (validite.End.HasValue && now > validite.End.Value)
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "BilletExpire",
                    HttpStatus = 400,
                    Message = $"La validité du billet a expiré le {validite.End:dd/MM/yyyy HH:mm}.",
                    IdReservation = res.IdReservation,
                    StatutReservation = res.StatutReservation,
                    DateDepartVoyage = voyageReference.DateDepart.Date,
                    HeureDepartVoyage = voyageReference.HeureDepart,
                    VoyageReference = voyageReference
                };
            }

            // Fenêtre basée sur le jour civil du départ (00:00 du jour DateDepart), pas sur l’heure affichée du voyage.
            var jourDepart = voyageReference.DateDepart.Date;
            var departPrevu = HoraireDepartLocal(voyageReference.DateDepart, voyageReference.HeureDepart);
            var ouverture = jourDepart - TimeSpan.FromHours(config.HeuresOuvertureEmbarquementAvantDepart);
            var fermeture = jourDepart + TimeSpan.FromHours(config.HeuresFermetureEmbarquementApresJourDepart);

            if (now < ouverture)
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "HorsFenetreEmbarquement",
                    HttpStatus = 400,
                    Message =
                        $"L'embarquement pour ce voyage n'est pas encore ouvert. Ouverture à partir du {ouverture:dd/MM/yyyy HH:mm} (heure locale).",
                    IdReservation = res.IdReservation,
                    StatutReservation = res.StatutReservation,
                    DateDepartVoyage = voyageReference.DateDepart.Date,
                    HeureDepartVoyage = voyageReference.HeureDepart,
                    VoyageReference = voyageReference
                };
            }

            if (now > fermeture)
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "HorsFenetreEmbarquement",
                    HttpStatus = 400,
                    Message =
                        $"La fenêtre d'embarquement pour ce voyage est close (départ le {departPrevu:dd/MM/yyyy HH:mm}, fin à {fermeture:dd/MM/yyyy HH:mm}).",
                    IdReservation = res.IdReservation,
                    StatutReservation = res.StatutReservation,
                    DateDepartVoyage = voyageReference.DateDepart.Date,
                    HeureDepartVoyage = voyageReference.HeureDepart,
                    VoyageReference = voyageReference
                };
            }

            return new BilletEligibiliteResult
            {
                Autorise = true,
                Code = "Valide",
                HttpStatus = 200,
                Message =
                    "Ce billet est valide pour l'embarquement (réservation confirmée, voyage dans la fenêtre autorisée).",
                IdReservation = res.IdReservation,
                StatutReservation = res.StatutReservation,
                DateDepartVoyage = voyageReference.DateDepart.Date,
                HeureDepartVoyage = voyageReference.HeureDepart,
                VoyageReference = voyageReference
            };
        }

        private async Task<BilletEligibiliteResult> EvaluerEligibiliteReaffectationAsync(Billet billet, int idVoyageCible)
        {
            if (billet.IsUsed)
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "DejaUtilise",
                    HttpStatus = 409,
                    Message = "Ce billet a déjà été utilisé (embarquement enregistré).",
                    IdReservation = billet.IdReservation,
                    StatutReservation = billet.Reservation?.StatutReservation
                };
            }

            if (await _context.BilletEmbarquements.AnyAsync(e => e.IdBillet == billet.IdBillet))
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "EmbarquementDejaEnregistre",
                    HttpStatus = 409,
                    Message = "Un enregistrement d'embarquement existe déjà pour ce billet.",
                    IdReservation = billet.IdReservation,
                    StatutReservation = billet.Reservation?.StatutReservation
                };
            }

            if (!billet.IdReservation.HasValue)
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "ReservationInvalide",
                    HttpStatus = 400,
                    Message = "Le billet doit être lié à une réservation pour être réaffecté.",
                    IdReservation = billet.IdReservation
                };
            }

            var res = billet.Reservation;
            if (res == null)
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "ReservationInvalide",
                    HttpStatus = 400,
                    Message = "La réservation associée à ce billet est introuvable.",
                    IdReservation = billet.IdReservation
                };
            }

            if (!res.Statut)
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "ReservationInactive",
                    HttpStatus = 400,
                    Message = "La réservation liée à ce billet est désactivée.",
                    IdReservation = res.IdReservation,
                    StatutReservation = res.StatutReservation
                };
            }

            if (EstReservationAnnulee(res.StatutReservation))
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "ReservationInvalide",
                    HttpStatus = 400,
                    Message = "La réservation a été annulée. Ce billet ne peut plus être réaffecté.",
                    IdReservation = res.IdReservation,
                    StatutReservation = res.StatutReservation
                };
            }

            if (EstReservationEnAttente(res.StatutReservation) || !EstReservationConfirmee(res.StatutReservation))
            {
                var msg = EstReservationEnAttente(res.StatutReservation)
                    ? "La réservation n'est pas encore confirmée. Le billet ne peut pas être réaffecté."
                    : $"Le statut de réservation « {res.StatutReservation} » ne permet pas la réaffectation.";
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "ReservationInvalide",
                    HttpStatus = 400,
                    Message = msg,
                    IdReservation = res.IdReservation,
                    StatutReservation = res.StatutReservation
                };
            }

            var voyageSource = res.Voyage;
            if (voyageSource == null || voyageSource.Statut == false)
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "VoyageIndisponible",
                    HttpStatus = 400,
                    Message = voyageSource == null
                        ? "Le voyage source associé à cette réservation est introuvable."
                        : "Le voyage source associé à cette réservation n'est plus disponible.",
                    IdReservation = res.IdReservation,
                    StatutReservation = res.StatutReservation
                };
            }

            if (res.IdVoyage == idVoyageCible)
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "VoyageIncompatible",
                    HttpStatus = 409,
                    Message = "Le billet est déjà affecté à ce voyage.",
                    IdReservation = res.IdReservation,
                    StatutReservation = res.StatutReservation
                };
            }

            var voyageCible = await _context.Voyages
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == idVoyageCible);

            if (voyageCible == null || voyageCible.Statut == false)
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "VoyageIndisponible",
                    HttpStatus = 400,
                    Message = "Le voyage cible est introuvable ou inactif.",
                    IdReservation = res.IdReservation,
                    StatutReservation = res.StatutReservation
                };
            }

            if (voyageCible.IdSociete != billet.IdSociete)
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "VoyageIncompatible",
                    HttpStatus = 400,
                    Message = "Le voyage cible n'appartient pas à la même société que le billet.",
                    IdReservation = res.IdReservation,
                    StatutReservation = res.StatutReservation
                };
            }

            if (voyageCible.IdDestination != voyageSource.IdDestination)
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "VoyageIncompatible",
                    HttpStatus = 400,
                    Message = "Le voyage cible n'a pas la même destination que le voyage d'origine du billet.",
                    IdReservation = res.IdReservation,
                    StatutReservation = res.StatutReservation
                };
            }

            var now = DateTime.Now;
            var departCible = HoraireDepartLocal(voyageCible.DateDepart, voyageCible.HeureDepart);
            if (departCible <= now)
            {
                return new BilletEligibiliteResult
                {
                    Autorise = false,
                    Code = "VoyageCibleParti",
                    HttpStatus = 409,
                    Message = "Le voyage cible a déjà départé. Réaffectation non autorisée.",
                    IdReservation = res.IdReservation,
                    StatutReservation = res.StatutReservation,
                    DateDepartVoyage = voyageCible.DateDepart.Date,
                    HeureDepartVoyage = voyageCible.HeureDepart,
                    VoyageReference = voyageCible
                };
            }

            return new BilletEligibiliteResult
            {
                Autorise = true,
                Code = "ReaffectationAutorisee",
                HttpStatus = 200,
                Message = "Ce billet peut être réaffecté vers le voyage cible.",
                IdReservation = res.IdReservation,
                StatutReservation = res.StatutReservation,
                DateDepartVoyage = voyageCible.DateDepart.Date,
                HeureDepartVoyage = voyageCible.HeureDepart,
                VoyageReference = voyageCible
            };
        }

        /// <inheritdoc />
        public async Task<BilletCheckResponseDto> CheckBilletAsync(int idBillet, int? idVoyageCible = null)
        {
            var billet = await GetByIdAsync(idBillet);
            return await CheckBilletCoreAsync(billet, idVoyageCible);
        }

        /// <inheritdoc />
        public async Task<BilletCheckResponseDto> CheckBilletByQrCodeAsync(string qrCode, int? idVoyageCible = null)
        {
            if (string.IsNullOrWhiteSpace(qrCode))
                return BilletCheckNonReconnu();

            var normalized = qrCode.Trim();
            var billet = await QueryBilletsWithEmbarquementIncludes()
                .FirstOrDefaultAsync(b => b.QrCode == normalized);
            return await CheckBilletCoreAsync(billet, idVoyageCible);
        }

        private static BilletCheckResponseDto BilletCheckNonReconnu() =>
            new()
            {
                IdBillet = null,
                IsUsed = null,
                Statut = "NonReconnu",
                Message =
                    "Ce billet ne correspond à aucun titre enregistré dans notre système. Il peut s'agir d'une contrefaçon ou d'un code invalide.",
                EmbarquementAutorise = false
            };

        private static (DateTime? Start, DateTime? End) ResolveBilletValidityWindow(
            Billet billet,
            Voyage? voyageOrigine,
            int dureeValiditeBilletJours) =>
            BilletValidityHelper.ResolveWindow(billet, voyageOrigine, dureeValiditeBilletJours);

        private async Task<BilletCheckResponseDto> CheckBilletCoreAsync(Billet? billet, int? idVoyageCible = null)
        {
            if (billet == null)
                return BilletCheckNonReconnu();

            var elig = await EvaluerEligibiliteEmbarquementAsync(billet, idVoyageCible);
            return ToCheckResponseDto(billet, elig);
        }


        // --- Embarquement ---

        public async Task<BilletEmbarquementOperationResult> EnregistrerEmbarquementAsync(
            int idSociete,
            int idBillet,
            int idReservationPassenger,
            int? idVoyageCible,
            int? idUtilisateurEnregistrement)
        {
            // MySqlRetryingExecutionStrategy : les transactions utilisateur doivent être dans ExecuteAsync.
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();

                var billet = await GetByIdAsync(idBillet);
                if (billet == null)
                {
                    return new BilletEmbarquementOperationResult
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = $"Billet {idBillet} introuvable."
                    };
                }

                if (billet.IdSociete != idSociete)
                {
                    return new BilletEmbarquementOperationResult
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "Le billet n'appartient pas à cette société."
                    };
                }

                var elig = await EvaluerEligibiliteEmbarquementAsync(billet, idVoyageCible);
                if (!elig.Autorise)
                {
                    return new BilletEmbarquementOperationResult
                    {
                        Success = false,
                        StatusCode = elig.HttpStatus,
                        Message = elig.Message
                    };
                }

                if (!billet.IdReservationPassenger.HasValue || billet.IdReservationPassenger.Value != idReservationPassenger)
                {
                    return new BilletEmbarquementOperationResult
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "Le billet ne correspond pas au passager indiqué."
                    };
                }

                var passenger = await _context.ReservationPassengers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdReservationPassenger == idReservationPassenger);

                if (passenger == null)
                {
                    return new BilletEmbarquementOperationResult
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = $"Passager de réservation {idReservationPassenger} introuvable."
                    };
                }

                if (passenger.IdSociete != idSociete)
                {
                    return new BilletEmbarquementOperationResult
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "Le passager n'appartient pas à cette société."
                    };
                }

                if (billet.IdReservation.HasValue && billet.IdReservation.Value != passenger.IdReservation)
                {
                    return new BilletEmbarquementOperationResult
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "Incohérence entre le billet et la réservation du passager."
                    };
                }

                var nowUtc = DateTime.UtcNow;
                billet.IsUsed = true;
                billet.DateModification = DateTime.Now;

                var histoire = new BilletEmbarquement
                {
                    IdSociete = idSociete,
                    IdBillet = idBillet,
                    IdReservationPassenger = idReservationPassenger,
                    DateEmbarquementUtc = nowUtc,
                    IdUtilisateurEnregistrement = idUtilisateurEnregistrement
                };
                _context.BilletEmbarquements.Add(histoire);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex) when (IsDuplicateBilletEmbarquementIndex(ex))
                {
                    _logger.LogWarning(ex,
                        "Course sur embarquement — contrainte unique IdBillet (billet {IdBillet}, passager {IdPassager})",
                        idBillet, idReservationPassenger);
                    return new BilletEmbarquementOperationResult
                    {
                        Success = false,
                        StatusCode = 409,
                        Message =
                            "Un enregistrement d'embarquement existe déjà pour ce billet (requête concurrente ou doublon)."
                    };
                }

                await tx.CommitAsync();

                var billetComplet = await GetByIdAsync(idBillet);

                _logger.LogInformation(
                    "Embarquement enregistré — billet {IdBillet}, passager {IdPassager}, société {IdSociete}",
                    idBillet, idReservationPassenger, idSociete);

                return new BilletEmbarquementOperationResult
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "Embarquement enregistré.",
                    Billet = billetComplet,
                    Embarquement = histoire
                };
            });
        }
    }
}
