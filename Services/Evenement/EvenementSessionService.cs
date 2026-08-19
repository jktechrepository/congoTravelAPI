using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement
{
    public class EvenementSessionService : IEvenementSessionService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IEvenementSessionPhotoService _photoService;
        private readonly ILogger<EvenementSessionService> _logger;

        public EvenementSessionService(
            CongoTravelDbContext context,
            IEvenementSessionPhotoService photoService,
            ILogger<EvenementSessionService> logger)
        {
            _context = context;
            _photoService = photoService;
            _logger = logger;
        }

        public async Task<EvenementSessionResponseDto> CreateDraftAsync(
            EvenementCreateSessionRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var inventoryMode = ParseInventoryMode(request.InventoryMode);
            var typeEvenement = ParseTypeEvenement(request.TypeEvenement);
            request.StartAtUtc = EvenementDateTimeUtcHelper.NormalizeToUtc(request.StartAtUtc);
            request.EndAtUtc = EvenementDateTimeUtcHelper.NormalizeToUtc(request.EndAtUtc);
            ValidateCreateRequest(request, inventoryMode);

            if (request.IdSite <= 0)
            {
                throw new InvalidOperationException("IdSite est obligatoire pour créer une session événement.");
            }

            await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(
                _context, request.IdSite, idSociete, cancellationToken);

            var codeSession = request.CodeSession.Trim();
            var exists = await _context.EvenementSessions
                .AsNoTracking()
                .AnyAsync(
                    s => s.IdSociete == idSociete && s.CodeSession == codeSession,
                    cancellationToken);

            if (exists)
            {
                throw new EvenementSessionConflictException(
                    $"Une session avec le code '{codeSession}' existe déjà pour cette société.");
            }

            var utcNow = DateTime.UtcNow;
            var session = new EvenementSession
            {
                IdSociete = idSociete,
                IdSite = request.IdSite,
                CodeSession = codeSession,
                Libelle = request.Libelle.Trim(),
                Description = NormalizeOptionalText(request.Description),
                StartAtUtc = request.StartAtUtc,
                EndAtUtc = request.EndAtUtc,
                InventoryMode = inventoryMode,
                TypeEvenement = typeEvenement,
                NomOrganisateur = NormalizeOptionalText(request.NomOrganisateur),
                TelephoneOrganisateur = NormalizeOptionalText(request.TelephoneOrganisateur),
                MailOrganisateur = NormalizeOptionalEmail(request.MailOrganisateur),
                LogoOrganisateur = NormalizeOptionalText(request.LogoOrganisateur),
                Ville = NormalizeOptionalText(request.Ville),
                Commune = NormalizeOptionalText(request.Commune),
                Quartier = NormalizeOptionalText(request.Quartier),
                Avenue = NormalizeOptionalText(request.Avenue),
                Numero = NormalizeOptionalText(request.Numero),
                Status = EvenementSessionStatus.Draft,
                DateCreation = utcNow
            };

            switch (inventoryMode)
            {
                case EvenementInventoryMode.GlobalQuota:
                    AttachGlobalQuota(session, request.GlobalQuota!);
                    break;

                case EvenementInventoryMode.ClassQuota:
                    await AttachClassQuotasAsync(
                        session, request.ClassQuotas!, idSociete, cancellationToken);
                    break;

                case EvenementInventoryMode.SeatNumbered:
                    await AttachSeatPlanAsync(
                        session, request.Sections, request.Seats, idSociete, cancellationToken);
                    break;
            }

            _context.EvenementSessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);

            await _photoService.AddPhotosOnCreateAsync(
                session.IdEvenementSession,
                idSociete,
                request.Photos,
                cancellationToken);

            _logger.LogInformation(
                "Session événement Draft créée — Id={Id}, Societe={IdSociete}, Code={Code}, Mode={Mode}",
                session.IdEvenementSession,
                idSociete,
                codeSession,
                inventoryMode);

            return await LoadSessionResponseAsync(session.IdEvenementSession, idSociete, cancellationToken);
        }

        public async Task<EvenementSessionResponseDto?> GetByIdAsync(
            int idEvenementSession,
            int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            var query = SessionDetailQuery()
                .Where(s => s.IdEvenementSession == idEvenementSession);

            if (idSociete.HasValue && idSociete.Value > 0)
                query = query.Where(s => s.IdSociete == idSociete.Value);

            var session = await query.FirstOrDefaultAsync(cancellationToken);
            return session == null ? null : EvenementSessionMapper.ToResponseDto(session);
        }

        public async Task<EvenementSessionResponseDto?> GetPublishedByIdAsync(
            int idEvenementSession,
            CancellationToken cancellationToken = default)
        {
            var session = await SessionDetailQuery()
                .FirstOrDefaultAsync(
                    s => s.IdEvenementSession == idEvenementSession
                         && s.Status == EvenementSessionStatus.Published
                         && s.Societe != null
                         && s.Societe.Statut == true,
                    cancellationToken);

            return session == null ? null : EvenementSessionMapper.ToResponseDto(session);
        }

        public async Task<EvenementSessionResponseDto?> GetByCodeAsync(
            string codeSession,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(codeSession))
                return null;

            var normalized = codeSession.Trim();
            var session = await SessionDetailQuery()
                .FirstOrDefaultAsync(
                    s => s.IdSociete == idSociete && s.CodeSession == normalized,
                    cancellationToken);

            return session == null ? null : EvenementSessionMapper.ToResponseDto(session);
        }

        public async Task<EvenementSessionResponseDto?> GetPublishedByCodeAsync(
            string codeSession,
            int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(codeSession))
                return null;

            var normalized = codeSession.Trim();
            var query = SessionDetailQuery()
                .Where(s =>
                    s.CodeSession == normalized
                    && s.Status == EvenementSessionStatus.Published
                    && s.Societe != null
                    && s.Societe.Statut == true);

            if (idSociete.HasValue && idSociete.Value > 0)
            {
                var session = await query
                    .FirstOrDefaultAsync(s => s.IdSociete == idSociete.Value, cancellationToken);
                return session == null ? null : EvenementSessionMapper.ToResponseDto(session);
            }

            var matches = await query.Take(2).ToListAsync(cancellationToken);
            if (matches.Count == 0)
                return null;

            if (matches.Count > 1)
            {
                throw new ArgumentException(
                    $"Plusieurs sessions Published portent le code '{normalized}'. Précisez ?idSociete=.");
            }

            return EvenementSessionMapper.ToResponseDto(matches[0]);
        }

        public async Task<IReadOnlyList<EvenementSessionListItemDto>> ListAsync(
            int idSociete,
            EvenementSessionListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var sessions = await BuildListQuery(idSociete, filter)
                .ToListAsync(cancellationToken);

            return sessions
                .Select(EvenementSessionMapper.ToListItemDto)
                .ToList();
        }

        public async Task<IReadOnlyList<EvenementSessionListItemDto>> ListPublishedGlobalAsync(
            EvenementSessionListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;
            var query = SessionListQuery()
                .Where(s => s.Status == EvenementSessionStatus.Published
                            && s.Societe != null
                            && s.Societe.Statut == true
                            && ((s.EndAtUtc.HasValue && s.EndAtUtc > utcNow)
                                || (!s.EndAtUtc.HasValue && s.StartAtUtc.AddHours(24) > utcNow)));

            if (filter?.IdSociete.HasValue == true && filter.IdSociete.Value > 0)
                query = query.Where(s => s.IdSociete == filter.IdSociete.Value);

            if (filter?.InventoryMode.HasValue == true)
                query = query.Where(s => s.InventoryMode == filter.InventoryMode.Value);

            if (filter?.TypeEvenement.HasValue == true)
                query = query.Where(s => s.TypeEvenement == filter.TypeEvenement.Value);

            var sessions = await query
                .OrderBy(s => s.StartAtUtc)
                .ToListAsync(cancellationToken);

            return sessions
                .Select(EvenementSessionMapper.ToListItemDto)
                .ToList();
        }

        public Task<IReadOnlyList<EvenementSessionListItemDto>> ListByStatusAsync(
            EvenementSessionStatus status,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            ListAsync(
                idSociete,
                new EvenementSessionListFilter { Status = status },
                cancellationToken);

        public Task<IReadOnlyList<EvenementSessionListItemDto>> ListByInventoryModeAsync(
            EvenementInventoryMode inventoryMode,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            ListAsync(
                idSociete,
                new EvenementSessionListFilter { InventoryMode = inventoryMode },
                cancellationToken);

        public async Task<IReadOnlyList<EvenementSessionListItemDto>> ListByDateAsync(
            DateTime date,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var day = date.Date;
            var sessions = await SessionListQuery()
                .Where(s => s.IdSociete == idSociete && s.StartAtUtc.Date == day)
                .OrderByDescending(s => s.StartAtUtc)
                .ToListAsync(cancellationToken);

            return sessions
                .Select(EvenementSessionMapper.ToListItemDto)
                .ToList();
        }

        public async Task<IReadOnlyList<EvenementSessionListItemDto>> ListByDateRangeAsync(
            DateTime dateDebut,
            DateTime dateFin,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var start = dateDebut.Date;
            var end = dateFin.Date.AddDays(1).AddTicks(-1);

            var sessions = await SessionListQuery()
                .Where(s => s.IdSociete == idSociete && s.StartAtUtc >= start && s.StartAtUtc <= end)
                .OrderByDescending(s => s.StartAtUtc)
                .ToListAsync(cancellationToken);

            return sessions
                .Select(EvenementSessionMapper.ToListItemDto)
                .ToList();
        }

        public async Task<EvenementSessionResponseDto> PublishAsync(
            int idEvenementSession,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var session = await _context.EvenementSessions
                .Include(s => s.GlobalQuota)
                .Include(s => s.ClassQuotas)
                    .ThenInclude(q => q.Classe)
                .Include(s => s.Seats)
                .FirstOrDefaultAsync(
                    s => s.IdEvenementSession == idEvenementSession && s.IdSociete == idSociete,
                    cancellationToken);

            if (session == null)
                throw new KeyNotFoundException($"Session événement {idEvenementSession} introuvable.");

            if (session.Status != EvenementSessionStatus.Draft)
            {
                throw new InvalidOperationException(
                    $"Seule une session Draft peut être publiée (statut actuel : {session.Status}).");
            }

            ValidateInventoryForPublish(session);

            session.Status = EvenementSessionStatus.Published;
            session.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Session événement publiée — Id={Id}, Societe={IdSociete}, Mode={Mode}",
                session.IdEvenementSession,
                idSociete,
                session.InventoryMode);

            return await LoadSessionResponseAsync(session.IdEvenementSession, idSociete, cancellationToken);
        }

        private IQueryable<EvenementSession> BuildListQuery(
            int idSociete,
            EvenementSessionListFilter? filter)
        {
            var query = SessionListQuery()
                .Where(s => s.IdSociete == idSociete);

            if (filter?.Status.HasValue == true)
                query = query.Where(s => s.Status == filter.Status.Value);

            if (filter?.InventoryMode.HasValue == true)
                query = query.Where(s => s.InventoryMode == filter.InventoryMode.Value);

            if (filter?.TypeEvenement.HasValue == true)
                query = query.Where(s => s.TypeEvenement == filter.TypeEvenement.Value);

            return query.OrderByDescending(s => s.StartAtUtc);
        }

        /// <summary>Includes pour listes enrichies (société, couverture, résumé prix).</summary>
        private IQueryable<EvenementSession> SessionListQuery() =>
            _context.EvenementSessions
                .AsNoTracking()
                .Include(s => s.Societe)
                .Include(s => s.Site)
                .Include(s => s.Photos)
                .Include(s => s.GlobalQuota)
                .Include(s => s.ClassQuotas)
                .Include(s => s.Seats);

        private IQueryable<EvenementSession> SessionDetailQuery() =>
            _context.EvenementSessions
                .AsNoTracking()
                .Include(s => s.Societe)
                .Include(s => s.Site)
                .Include(s => s.GlobalQuota)
                .Include(s => s.ClassQuotas)
                    .ThenInclude(q => q.Classe)
                .Include(s => s.Seats)
                    .ThenInclude(seat => seat.Section)
                .Include(s => s.Seats)
                    .ThenInclude(seat => seat.Classe)
                .Include(s => s.Photos);

        private async Task<EvenementSessionResponseDto> LoadSessionResponseAsync(
            int idEvenementSession,
            int idSociete,
            CancellationToken cancellationToken)
        {
            var session = await SessionDetailQuery()
                .FirstAsync(
                    s => s.IdEvenementSession == idEvenementSession && s.IdSociete == idSociete,
                    cancellationToken);

            return EvenementSessionMapper.ToResponseDto(session);
        }

        private static void AttachGlobalQuota(
            EvenementSession session,
            EvenementCreateSessionGlobalQuotaDto global)
        {
            session.GlobalQuota = new EvenementSessionGlobalQuota
            {
                CapaciteTotale = global.CapaciteTotale,
                QuantiteHold = 0,
                QuantiteVendue = 0,
                PrixUnitaire = global.PrixUnitaire,
                CodeDevise = NormalizeCodeDevise(global.CodeDevise)
            };
        }

        private async Task AttachClassQuotasAsync(
            EvenementSession session,
            IReadOnlyList<EvenementCreateSessionClassQuotaDto> classQuotas,
            int idSociete,
            CancellationToken cancellationToken)
        {
            var classeIds = classQuotas.Select(q => q.IdEvenementClasse).Distinct().ToList();
            var classes = await _context.EvenementClasses
                .AsNoTracking()
                .Where(c => c.IdSociete == idSociete && classeIds.Contains(c.IdEvenementClasse))
                .ToListAsync(cancellationToken);

            if (classes.Count != classeIds.Count)
            {
                throw new InvalidOperationException(
                    "Une ou plusieurs classes référencées sont introuvables pour cette société.");
            }

            if (classes.Any(c => !c.Statut))
            {
                throw new InvalidOperationException(
                    "Une ou plusieurs classes référencées sont inactives.");
            }

            foreach (var item in classQuotas)
            {
                session.ClassQuotas.Add(new EvenementSessionClassQuota
                {
                    IdEvenementClasse = item.IdEvenementClasse,
                    CapaciteTotale = item.CapaciteTotale,
                    QuantiteHold = 0,
                    QuantiteVendue = 0,
                    PrixUnitaire = item.PrixUnitaire,
                    CodeDevise = NormalizeCodeDevise(item.CodeDevise)
                });
            }
        }

        private async Task AttachSeatPlanAsync(
            EvenementSession session,
            IReadOnlyList<EvenementCreateSessionSectionDto>? sections,
            IReadOnlyList<EvenementCreateSessionSeatDto>? standaloneSeats,
            int idSociete,
            CancellationToken cancellationToken)
        {
            var classeIds = CollectReferencedClasseIds(sections, standaloneSeats);
            var classesById = await LoadActiveClassesByIdAsync(classeIds, idSociete, cancellationToken);
            var seatCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (sections != null)
            {
                var sectionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var sectionDto in sections)
                {
                    var codeSection = sectionDto.CodeSection.Trim();
                    if (string.IsNullOrWhiteSpace(codeSection))
                        throw new InvalidOperationException("CodeSection est obligatoire.");

                    if (!sectionCodes.Add(codeSection))
                    {
                        throw new InvalidOperationException(
                            $"Sections contient un doublon pour CodeSection='{codeSection}'.");
                    }

                    var section = new EvenementSessionSection
                    {
                        CodeSection = codeSection,
                        Libelle = sectionDto.Libelle.Trim()
                    };

                    foreach (var seatDto in sectionDto.Seats)
                    {
                        var seat = CreateSessionSeat(seatDto, classesById, seatCodes);
                        seat.Section = section;
                        session.Seats.Add(seat);
                    }

                    session.Sections.Add(section);
                }
            }

            if (standaloneSeats != null)
            {
                foreach (var seatDto in standaloneSeats)
                {
                    session.Seats.Add(CreateSessionSeat(seatDto, classesById, seatCodes));
                }
            }
        }

        private static HashSet<int> CollectReferencedClasseIds(
            IReadOnlyList<EvenementCreateSessionSectionDto>? sections,
            IReadOnlyList<EvenementCreateSessionSeatDto>? standaloneSeats)
        {
            var classeIds = new HashSet<int>();
            if (sections != null)
            {
                foreach (var section in sections)
                {
                    foreach (var seat in section.Seats.Where(s => s.IdEvenementClasse.HasValue))
                        classeIds.Add(seat.IdEvenementClasse!.Value);
                }
            }

            if (standaloneSeats != null)
            {
                foreach (var seat in standaloneSeats.Where(s => s.IdEvenementClasse.HasValue))
                    classeIds.Add(seat.IdEvenementClasse!.Value);
            }

            return classeIds;
        }

        private async Task<Dictionary<int, EvenementClasse>> LoadActiveClassesByIdAsync(
            IEnumerable<int> classeIds,
            int idSociete,
            CancellationToken cancellationToken)
        {
            var ids = classeIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<int, EvenementClasse>();

            var classes = await _context.EvenementClasses
                .AsNoTracking()
                .Where(c => c.IdSociete == idSociete && ids.Contains(c.IdEvenementClasse))
                .ToListAsync(cancellationToken);

            if (classes.Count != ids.Count)
            {
                throw new InvalidOperationException(
                    "Une ou plusieurs classes référencées sont introuvables pour cette société.");
            }

            if (classes.Any(c => !c.Statut))
            {
                throw new InvalidOperationException(
                    "Une ou plusieurs classes référencées sont inactives.");
            }

            return classes.ToDictionary(c => c.IdEvenementClasse);
        }

        private static EvenementSessionSeat CreateSessionSeat(
            EvenementCreateSessionSeatDto seatDto,
            IReadOnlyDictionary<int, EvenementClasse> classesById,
            ISet<string> seatCodes)
        {
            var seatCode = seatDto.SeatCode.Trim();
            if (string.IsNullOrWhiteSpace(seatCode))
                throw new InvalidOperationException("SeatCode est obligatoire.");

            if (!seatCodes.Add(seatCode))
            {
                throw new InvalidOperationException(
                    $"Le plan de salle contient un doublon pour SeatCode='{seatCode}'.");
            }

            if (seatDto.PrixUnitaire < 0)
                throw new InvalidOperationException($"PrixUnitaire invalide pour SeatCode='{seatCode}'.");

            if (seatDto.IdEvenementClasse.HasValue && !classesById.ContainsKey(seatDto.IdEvenementClasse.Value))
            {
                throw new InvalidOperationException(
                    $"Classe {seatDto.IdEvenementClasse.Value} introuvable pour SeatCode='{seatCode}'.");
            }

            return new EvenementSessionSeat
            {
                SeatCode = seatCode,
                IdEvenementClasse = seatDto.IdEvenementClasse,
                SeatStatus = EvenementSessionSeatStatus.Available,
                PrixUnitaire = seatDto.PrixUnitaire,
                CodeDevise = NormalizeCodeDevise(seatDto.CodeDevise)
            };
        }

        private static void ValidateCreateRequest(
            EvenementCreateSessionRequestDto request,
            EvenementInventoryMode inventoryMode)
        {
            if (string.IsNullOrWhiteSpace(request.CodeSession))
                throw new InvalidOperationException("CodeSession est obligatoire.");

            if (string.IsNullOrWhiteSpace(request.Libelle))
                throw new InvalidOperationException("Libelle est obligatoire.");

            if (request.EndAtUtc.HasValue && request.EndAtUtc.Value < request.StartAtUtc)
            {
                throw new InvalidOperationException(
                    "EndAtUtc doit être postérieur ou égal à StartAtUtc.");
            }

            switch (inventoryMode)
            {
                case EvenementInventoryMode.GlobalQuota:
                    ValidateGlobalQuotaCreate(request.GlobalQuota);
                    break;

                case EvenementInventoryMode.ClassQuota:
                    ValidateClassQuotasCreate(request.ClassQuotas);
                    break;

                case EvenementInventoryMode.SeatNumbered:
                    ValidateSeatPlanCreate(request.Sections, request.Seats);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"InventoryMode {inventoryMode} non supporté pour la création.");
            }
        }

        private static void ValidateGlobalQuotaCreate(EvenementCreateSessionGlobalQuotaDto? global)
        {
            if (global == null)
                throw new InvalidOperationException("GlobalQuota est obligatoire pour InventoryMode GlobalQuota.");

            if (global.CapaciteTotale <= 0)
                throw new InvalidOperationException("CapaciteTotale doit être strictement positive.");

            if (global.PrixUnitaire < 0)
                throw new InvalidOperationException("PrixUnitaire ne peut pas être négatif.");
        }

        private static EvenementSessionType ParseTypeEvenement(string? typeEvenement)
        {
            if (string.IsNullOrWhiteSpace(typeEvenement))
                return EvenementSessionType.Autres;

            if (Enum.TryParse<EvenementSessionType>(typeEvenement.Trim(), ignoreCase: true, out var value))
                return value;

            throw new InvalidOperationException(
                $"TypeEvenement invalide '{typeEvenement}'. Valeurs acceptées : Sport, Music, Art, Cinema, Formation, Conference, Spectacle, Festival, Autres.");
        }

        private static string? NormalizeOptionalText(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string? NormalizeOptionalEmail(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static void ValidateSeatPlanCreate(
            List<EvenementCreateSessionSectionDto>? sections,
            List<EvenementCreateSessionSeatDto>? standaloneSeats)
        {
            var sectionSeatCount = sections?.Sum(s => s.Seats?.Count ?? 0) ?? 0;
            var standaloneCount = standaloneSeats?.Count ?? 0;
            var totalSeats = sectionSeatCount + standaloneCount;

            if (totalSeats <= 0)
            {
                throw new InvalidOperationException(
                    "SeatNumbered : au moins un siège est requis (sections et/ou seats).");
            }
        }

        private static void ValidateClassQuotasCreate(List<EvenementCreateSessionClassQuotaDto>? classQuotas)
        {
            if (classQuotas == null || classQuotas.Count == 0)
            {
                throw new InvalidOperationException(
                    "ClassQuotas est obligatoire pour InventoryMode ClassQuota (au moins une classe).");
            }

            var duplicateClasse = classQuotas
                .GroupBy(q => q.IdEvenementClasse)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicateClasse != null)
            {
                throw new InvalidOperationException(
                    $"ClassQuotas contient un doublon pour IdEvenementClasse={duplicateClasse.Key}.");
            }

            foreach (var quota in classQuotas)
            {
                if (quota.CapaciteTotale <= 0)
                {
                    throw new InvalidOperationException(
                        $"CapaciteTotale invalide pour IdEvenementClasse={quota.IdEvenementClasse}.");
                }

                if (quota.PrixUnitaire < 0)
                {
                    throw new InvalidOperationException(
                        $"PrixUnitaire invalide pour IdEvenementClasse={quota.IdEvenementClasse}.");
                }
            }
        }

        private static void ValidateInventoryForPublish(EvenementSession session)
        {
            switch (session.InventoryMode)
            {
                case EvenementInventoryMode.GlobalQuota:
                    if (session.GlobalQuota == null || session.GlobalQuota.CapaciteTotale <= 0)
                    {
                        throw new InvalidOperationException(
                            "Publication impossible : quota global manquant ou capacité invalide.");
                    }

                    return;

                case EvenementInventoryMode.ClassQuota:
                    if (session.ClassQuotas.Count == 0
                        || session.ClassQuotas.All(q => q.CapaciteTotale <= 0))
                    {
                        throw new InvalidOperationException(
                            "Publication impossible : au moins un quota classe valide est requis.");
                    }

                    return;

                case EvenementInventoryMode.SeatNumbered:
                    if (session.Seats.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "Publication impossible : au moins un siège est requis.");
                    }

                    return;

                default:
                    throw new InvalidOperationException(
                        $"Publication Mode {session.InventoryMode} : non implémentée.");
            }
        }

        private static EvenementInventoryMode ParseInventoryMode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return EvenementInventoryMode.GlobalQuota;

            if (!Enum.TryParse<EvenementInventoryMode>(value.Trim(), ignoreCase: true, out var mode))
            {
                throw new InvalidOperationException(
                    $"InventoryMode invalide : '{value}'. Valeurs : SeatNumbered, ClassQuota, GlobalQuota.");
            }

            return mode;
        }

        private static string NormalizeCodeDevise(string codeDevise)
        {
            var normalized = string.IsNullOrWhiteSpace(codeDevise)
                ? "CDF"
                : codeDevise.Trim().ToUpperInvariant();

            if (normalized is not ("CDF" or "USD"))
            {
                throw new InvalidOperationException(
                    "CodeDevise invalide. Valeurs acceptées : CDF, USD.");
            }

            return normalized;
        }
    }
}
