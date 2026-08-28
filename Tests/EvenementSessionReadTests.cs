using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Evenement;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementSessionReadTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static EvenementSessionService CreateService(CongoTravelDbContext ctx) =>
            PhotoStorageTestFactory.CreateEvenementSessionService(ctx);

        [Fact]
        public async Task ListAsync_filters_by_status_and_inventory_mode()
        {
            await using var ctx = BuildDb(nameof(ListAsync_filters_by_status_and_inventory_mode));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var draft = await service.CreateDraftAsync(BuildValidCreateRequest("DRAFT-1", idSite), idSociete);
            await service.PublishAsync(draft.IdEvenementSession, idSociete);

            var classSession = await service.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "CLASS-1",
                IdSite = idSite,
                Libelle = "Session classe",
                StartAtUtc = DateTime.UtcNow.AddDays(20),
                InventoryMode = "ClassQuota",
                ClassQuotas = new List<EvenementCreateSessionClassQuotaDto>
                {
                    new()
                    {
                        IdEvenementClasse = await SeedClasseAsync(ctx, idSociete),
                        CapaciteTotale = 30,
                        PrixUnitaire = 15m,
                        CodeDevise = "CDF"
                    }
                }
            }, idSociete);

            var published = await service.ListAsync(
                idSociete,
                new EvenementSessionListFilter
                {
                    Status = EvenementSessionStatus.Published,
                    InventoryMode = EvenementInventoryMode.GlobalQuota
                });

            Assert.Single(published);
            Assert.Equal("DRAFT-1", published[0].CodeSession);

            var drafts = await service.ListAsync(
                idSociete,
                new EvenementSessionListFilter { Status = EvenementSessionStatus.Draft });

            Assert.Single(drafts);
            Assert.Equal(classSession.IdEvenementSession, drafts[0].IdEvenementSession);
        }

        [Fact]
        public async Task GetByCodeAsync_returns_session_for_own_societe()
        {
            await using var ctx = BuildDb(nameof(GetByCodeAsync_returns_session_for_own_societe));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            await service.CreateDraftAsync(BuildValidCreateRequest("CODE-READ", idSite), idSociete);
            var found = await service.GetByCodeAsync("CODE-READ", idSociete);
            var missing = await service.GetByCodeAsync("CODE-READ", idSociete + 999);

            Assert.NotNull(found);
            Assert.Equal("CODE-READ", found!.CodeSession);
            Assert.Null(missing);
        }

        [Fact]
        public async Task ListByDateRangeAsync_returns_sessions_in_range()
        {
            await using var ctx = BuildDb(nameof(ListByDateRangeAsync_returns_sessions_in_range));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            await service.CreateDraftAsync(BuildValidCreateRequest("FUTURE", idSite), idSociete);

            var oldSession = new EvenementSession
            {
                IdSociete = idSociete,
                CodeSession = "OLD-SESSION",
                Libelle = "Ancienne session",
                StartAtUtc = new DateTime(2026, 1, 15, 18, 0, 0, DateTimeKind.Utc),
                InventoryMode = EvenementInventoryMode.GlobalQuota,
                Status = EvenementSessionStatus.Closed,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementSessions.Add(oldSession);
            await ctx.SaveChangesAsync();

            var recent = await service.ListByDateRangeAsync(
                DateTime.UtcNow.Date.AddDays(-1),
                DateTime.UtcNow.Date.AddDays(30),
                idSociete);
            var oldOnly = await service.ListByDateRangeAsync(
                new DateTime(2026, 1, 15),
                new DateTime(2026, 1, 15),
                idSociete);

            Assert.Single(recent);
            Assert.Equal("FUTURE", recent[0].CodeSession);
            Assert.Single(oldOnly);
            Assert.Equal("OLD-SESSION", oldOnly[0].CodeSession);
        }

        [Fact]
        public async Task GetByIdAsync_still_returns_null_for_other_societe()
        {
            await using var ctx = BuildDb(nameof(GetByIdAsync_still_returns_null_for_other_societe));
            var (idSociete1, idSite1) = await SeedSocieteAsync(ctx, "Societe A");
            var (idSociete2, idSite2) = await SeedSocieteAsync(ctx, "Societe B");
            var service = CreateService(ctx);

            var created = await service.CreateDraftAsync(BuildValidCreateRequest("ISO-READ", idSite1), idSociete1);
            var other = await service.GetByIdAsync(created.IdEvenementSession, idSociete2);

            Assert.Null(other);
        }

        [Fact]
        public async Task ListPublishedGlobalAsync_returns_published_from_all_societes_only()
        {
            await using var ctx = BuildDb(nameof(ListPublishedGlobalAsync_returns_published_from_all_societes_only));
            var (idA, idSiteA) = await SeedSocieteAsync(ctx, "Societe A");
            var (idB, idSiteB) = await SeedSocieteAsync(ctx, "Societe B");
            var service = CreateService(ctx);

            var draftA = await service.CreateDraftAsync(BuildValidCreateRequest("A-DRAFT", idSiteA), idA);
            var pubA = await service.CreateDraftAsync(BuildValidCreateRequest("A-PUB", idSiteA), idA);
            await service.PublishAsync(pubA.IdEvenementSession, idA);

            var pubB = await service.CreateDraftAsync(BuildValidCreateRequest("B-PUB", idSiteB), idB);
            await service.PublishAsync(pubB.IdEvenementSession, idB);

            // Even with a Draft filter in the object, global list must stay Published-only
            var global = await service.ListPublishedGlobalAsync(new EvenementSessionListFilter
            {
                Status = EvenementSessionStatus.Draft
            });

            Assert.Equal(2, global.Count);
            Assert.All(global, s => Assert.Equal("Published", s.Status));
            Assert.Contains(global, s => s.CodeSession == "A-PUB" && s.IdSociete == idA);
            Assert.Contains(global, s => s.CodeSession == "B-PUB" && s.IdSociete == idB);
            Assert.DoesNotContain(global, s => s.IdEvenementSession == draftA.IdEvenementSession);
        }

        [Fact]
        public async Task ListPublishedGlobalAsync_includes_in_progress_excludes_ended()
        {
            await using var ctx = BuildDb(nameof(ListPublishedGlobalAsync_includes_in_progress_excludes_ended));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var future = await service.CreateDraftAsync(BuildValidCreateRequest("FUTURE-PUB", idSite), idSociete);
            await service.PublishAsync(future.IdEvenementSession, idSociete);

            var inProgress = await service.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "LIVE-PUB",
                IdSite = idSite,
                Libelle = "In progress",
                StartAtUtc = DateTime.UtcNow.AddHours(-1),
                EndAtUtc = DateTime.UtcNow.AddHours(4),
                InventoryMode = "GlobalQuota",
                GlobalQuota = new EvenementCreateSessionGlobalQuotaDto
                {
                    CapaciteTotale = 10,
                    PrixUnitaire = 5m,
                    CodeDevise = "CDF"
                }
            }, idSociete);
            await service.PublishAsync(inProgress.IdEvenementSession, idSociete);

            var ended = await service.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "ENDED-PUB",
                IdSite = idSite,
                Libelle = "Ended session",
                StartAtUtc = DateTime.UtcNow.AddHours(-5),
                EndAtUtc = DateTime.UtcNow.AddHours(-1),
                InventoryMode = "GlobalQuota",
                GlobalQuota = new EvenementCreateSessionGlobalQuotaDto
                {
                    CapaciteTotale = 10,
                    PrixUnitaire = 5m,
                    CodeDevise = "CDF"
                }
            }, idSociete);
            await service.PublishAsync(ended.IdEvenementSession, idSociete);

            var global = await service.ListPublishedGlobalAsync();

            Assert.Equal(2, global.Count);
            Assert.Contains(global, s => s.CodeSession == "FUTURE-PUB");
            Assert.Contains(global, s => s.CodeSession == "LIVE-PUB");
            Assert.DoesNotContain(global, s => s.CodeSession == "ENDED-PUB");
        }

        [Fact]
        public async Task ListPublishedGlobalAsync_orders_by_StartAtUtc_ascending()
        {
            await using var ctx = BuildDb(nameof(ListPublishedGlobalAsync_orders_by_StartAtUtc_ascending));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var inSevenDays = await service.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "WEEK-PUB",
                IdSite = idSite,
                Libelle = "In seven days",
                StartAtUtc = DateTime.UtcNow.AddDays(7),
                InventoryMode = "GlobalQuota",
                GlobalQuota = new EvenementCreateSessionGlobalQuotaDto
                {
                    CapaciteTotale = 10,
                    PrixUnitaire = 5m,
                    CodeDevise = "CDF"
                }
            }, idSociete);
            await service.PublishAsync(inSevenDays.IdEvenementSession, idSociete);

            var tomorrow = await service.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "TOMORROW-PUB",
                IdSite = idSite,
                Libelle = "Tomorrow",
                StartAtUtc = DateTime.UtcNow.AddDays(1),
                InventoryMode = "GlobalQuota",
                GlobalQuota = new EvenementCreateSessionGlobalQuotaDto
                {
                    CapaciteTotale = 10,
                    PrixUnitaire = 5m,
                    CodeDevise = "CDF"
                }
            }, idSociete);
            await service.PublishAsync(tomorrow.IdEvenementSession, idSociete);

            var live = await service.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "LIVE-PUB",
                IdSite = idSite,
                Libelle = "In progress",
                StartAtUtc = DateTime.UtcNow.AddHours(-1),
                EndAtUtc = DateTime.UtcNow.AddHours(4),
                InventoryMode = "GlobalQuota",
                GlobalQuota = new EvenementCreateSessionGlobalQuotaDto
                {
                    CapaciteTotale = 10,
                    PrixUnitaire = 5m,
                    CodeDevise = "CDF"
                }
            }, idSociete);
            await service.PublishAsync(live.IdEvenementSession, idSociete);

            var global = await service.ListPublishedGlobalAsync();

            Assert.Equal(3, global.Count);
            Assert.Equal("LIVE-PUB", global[0].CodeSession);
            Assert.Equal("TOMORROW-PUB", global[1].CodeSession);
            Assert.Equal("WEEK-PUB", global[2].CodeSession);
        }

        [Fact]
        public async Task ListPublishedGlobalAsync_respects_inventory_mode_filter()
        {
            await using var ctx = BuildDb(nameof(ListPublishedGlobalAsync_respects_inventory_mode_filter));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var globalQuota = await service.CreateDraftAsync(BuildValidCreateRequest("GQ-PUB", idSite), idSociete);
            await service.PublishAsync(globalQuota.IdEvenementSession, idSociete);

            var classDraft = await service.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "CQ-PUB",
                IdSite = idSite,
                Libelle = "Class published",
                StartAtUtc = DateTime.UtcNow.AddDays(8),
                InventoryMode = "ClassQuota",
                ClassQuotas = new List<EvenementCreateSessionClassQuotaDto>
                {
                    new()
                    {
                        IdEvenementClasse = await SeedClasseAsync(ctx, idSociete),
                        CapaciteTotale = 20,
                        PrixUnitaire = 12m,
                        CodeDevise = "CDF"
                    }
                }
            }, idSociete);
            await service.PublishAsync(classDraft.IdEvenementSession, idSociete);

            var filtered = await service.ListPublishedGlobalAsync(new EvenementSessionListFilter
            {
                InventoryMode = EvenementInventoryMode.GlobalQuota
            });

            Assert.Single(filtered);
            Assert.Equal("GQ-PUB", filtered[0].CodeSession);
        }

        [Fact]
        public async Task ListPublishedGlobalAsync_respects_type_evenement_filter()
        {
            await using var ctx = BuildDb(nameof(ListPublishedGlobalAsync_respects_type_evenement_filter));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var musicDraft = await service.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "MUSIC-PUB",
                IdSite = idSite,
                Libelle = "Music published",
                Description = " Un grand concert live ",
                StartAtUtc = DateTime.UtcNow.AddDays(6),
                InventoryMode = "GlobalQuota",
                TypeEvenement = "Music",
                NomOrganisateur = "Live Nation",
                TelephoneOrganisateur = "+243811111111",
                MailOrganisateur = "music@orga.cd",
                LogoOrganisateur = "https://cdn.example/music.png",
                Ville = " Kinshasa ",
                Commune = " Lingwala ",
                Quartier = " Quartier Test ",
                Avenue = " Avenue exemple ",
                Numero = " 12A ",
                GlobalQuota = new EvenementCreateSessionGlobalQuotaDto
                {
                    CapaciteTotale = 20,
                    PrixUnitaire = 15m,
                    CodeDevise = "CDF"
                }
            }, idSociete);
            await service.PublishAsync(musicDraft.IdEvenementSession, idSociete);

            var sportDraft = await service.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "SPORT-PUB",
                IdSite = idSite,
                Libelle = "Sport published",
                StartAtUtc = DateTime.UtcNow.AddDays(7),
                InventoryMode = "GlobalQuota",
                TypeEvenement = "Sport",
                GlobalQuota = new EvenementCreateSessionGlobalQuotaDto
                {
                    CapaciteTotale = 20,
                    PrixUnitaire = 15m,
                    CodeDevise = "CDF"
                }
            }, idSociete);
            await service.PublishAsync(sportDraft.IdEvenementSession, idSociete);

            var filtered = await service.ListPublishedGlobalAsync(new EvenementSessionListFilter
            {
                TypeEvenement = EvenementSessionType.Music
            });

            Assert.Single(filtered);
            Assert.Equal("MUSIC-PUB", filtered[0].CodeSession);
            Assert.Equal("Music", filtered[0].TypeEvenement);
            Assert.Equal("Un grand concert live", filtered[0].Description);
            Assert.Equal("Live Nation", filtered[0].NomOrganisateur);
            Assert.Equal("+243811111111", filtered[0].TelephoneOrganisateur);
            Assert.Equal("music@orga.cd", filtered[0].MailOrganisateur);
            Assert.Equal("https://cdn.example/music.png", filtered[0].LogoOrganisateur);
            Assert.Equal("Kinshasa", filtered[0].Ville);
            Assert.Equal("Lingwala", filtered[0].Commune);
            Assert.Equal("Quartier Test", filtered[0].Quartier);
            Assert.Equal("Avenue exemple", filtered[0].Avenue);
            Assert.Equal("12A", filtered[0].Numero);
        }

        [Fact]
        public async Task ListPublishedGlobalAsync_filters_by_optional_idSociete()
        {
            await using var ctx = BuildDb(nameof(ListPublishedGlobalAsync_filters_by_optional_idSociete));
            var (idA, idSiteA) = await SeedSocieteAsync(ctx, "Societe A");
            var (idB, idSiteB) = await SeedSocieteAsync(ctx, "Societe B");
            var service = CreateService(ctx);

            var pubA = await service.CreateDraftAsync(BuildValidCreateRequest("A-ONLY", idSiteA), idA);
            await service.PublishAsync(pubA.IdEvenementSession, idA);
            var pubB = await service.CreateDraftAsync(BuildValidCreateRequest("B-ONLY", idSiteB), idB);
            await service.PublishAsync(pubB.IdEvenementSession, idB);

            var filtered = await service.ListPublishedGlobalAsync(new EvenementSessionListFilter
            {
                IdSociete = idB
            });

            Assert.Single(filtered);
            Assert.Equal("B-ONLY", filtered[0].CodeSession);
            Assert.Equal(idB, filtered[0].IdSociete);
        }

        [Fact]
        public async Task ListPublishedGlobalAsync_excludes_sessions_of_inactive_societe()
        {
            await using var ctx = BuildDb(nameof(ListPublishedGlobalAsync_excludes_sessions_of_inactive_societe));
            var (idA, idSiteA) = await SeedSocieteAsync(ctx, "Societe Active");
            var (idB, idSiteB) = await SeedSocieteAsync(ctx, "Societe Inactive");
            var service = CreateService(ctx);

            var active = await service.CreateDraftAsync(BuildValidCreateRequest("ACTIVE-PUB", idSiteA), idA);
            await service.PublishAsync(active.IdEvenementSession, idA);

            var inactive = await service.CreateDraftAsync(BuildValidCreateRequest("INACTIVE-PUB", idSiteB), idB);
            await service.PublishAsync(inactive.IdEvenementSession, idB);

            var inactiveSociete = await ctx.Societes.FirstAsync(s => s.IdSociete == idB);
            inactiveSociete.Statut = false;
            await ctx.SaveChangesAsync();

            var global = await service.ListPublishedGlobalAsync();

            Assert.Single(global);
            Assert.Equal("ACTIVE-PUB", global[0].CodeSession);
            Assert.DoesNotContain(global, s => s.CodeSession == "INACTIVE-PUB");
        }

        [Fact]
        public async Task ListPublishedGlobalAsync_returns_empty_for_inactive_societe_id_filter()
        {
            await using var ctx = BuildDb(nameof(ListPublishedGlobalAsync_returns_empty_for_inactive_societe_id_filter));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx, "Societe Inactive");
            var service = CreateService(ctx);

            var created = await service.CreateDraftAsync(BuildValidCreateRequest("INACTIVE-ONLY", idSite), idSociete);
            await service.PublishAsync(created.IdEvenementSession, idSociete);

            var societe = await ctx.Societes.FirstAsync(s => s.IdSociete == idSociete);
            societe.Statut = false;
            await ctx.SaveChangesAsync();

            var filtered = await service.ListPublishedGlobalAsync(new EvenementSessionListFilter
            {
                IdSociete = idSociete
            });

            Assert.Empty(filtered);
        }

        [Fact]
        public async Task GetPublishedByIdAsync_returns_published_from_any_societe()
        {
            await using var ctx = BuildDb(nameof(GetPublishedByIdAsync_returns_published_from_any_societe));
            var (idA, idSiteA) = await SeedSocieteAsync(ctx, "Societe A");
            var (idB, idSiteB) = await SeedSocieteAsync(ctx, "Societe B");
            var service = CreateService(ctx);

            var draft = await service.CreateDraftAsync(BuildValidCreateRequest("PUB-X", idSiteA), idA);
            await service.PublishAsync(draft.IdEvenementSession, idA);

            var found = await service.GetPublishedByIdAsync(draft.IdEvenementSession);
            var missingOtherTenant = await service.GetByIdAsync(draft.IdEvenementSession, idB);

            Assert.NotNull(found);
            Assert.Equal(idA, found!.IdSociete);
            Assert.Equal("Published", found.Status);
            Assert.Null(missingOtherTenant);
        }

        [Fact]
        public async Task GetPublishedByIdAsync_returns_null_for_draft()
        {
            await using var ctx = BuildDb(nameof(GetPublishedByIdAsync_returns_null_for_draft));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var draft = await service.CreateDraftAsync(BuildValidCreateRequest("DRAFT-HIDE", idSite), idSociete);
            var publishedOnly = await service.GetPublishedByIdAsync(draft.IdEvenementSession);
            var byId = await service.GetByIdAsync(draft.IdEvenementSession, idSociete);

            Assert.Null(publishedOnly);
            Assert.NotNull(byId);
            Assert.Equal("Draft", byId!.Status);
        }

        [Fact]
        public async Task GetPublishedByIdAsync_returns_null_for_inactive_societe()
        {
            await using var ctx = BuildDb(nameof(GetPublishedByIdAsync_returns_null_for_inactive_societe));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx, "Societe Inactive");
            var service = CreateService(ctx);

            var created = await service.CreateDraftAsync(BuildValidCreateRequest("INACTIVE-ID", idSite), idSociete);
            await service.PublishAsync(created.IdEvenementSession, idSociete);

            var societe = await ctx.Societes.FirstAsync(s => s.IdSociete == idSociete);
            societe.Statut = false;
            await ctx.SaveChangesAsync();

            var publishedOnly = await service.GetPublishedByIdAsync(created.IdEvenementSession);

            Assert.Null(publishedOnly);
        }

        [Fact]
        public async Task GetByIdAsync_without_societe_returns_any_status()
        {
            await using var ctx = BuildDb(nameof(GetByIdAsync_without_societe_returns_any_status));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var draft = await service.CreateDraftAsync(BuildValidCreateRequest("SA-DRAFT", idSite), idSociete);
            var found = await service.GetByIdAsync(draft.IdEvenementSession);

            Assert.NotNull(found);
            Assert.Equal("Draft", found!.Status);
        }

        [Fact]
        public async Task GetPublishedByCodeAsync_requires_idSociete_when_ambiguous()
        {
            await using var ctx = BuildDb(nameof(GetPublishedByCodeAsync_requires_idSociete_when_ambiguous));
            var (idA, idSiteA) = await SeedSocieteAsync(ctx, "Societe A");
            var (idB, idSiteB) = await SeedSocieteAsync(ctx, "Societe B");
            var service = CreateService(ctx);

            var a = await service.CreateDraftAsync(BuildValidCreateRequest("SAME-CODE", idSiteA), idA);
            await service.PublishAsync(a.IdEvenementSession, idA);
            var b = await service.CreateDraftAsync(BuildValidCreateRequest("SAME-CODE", idSiteB), idB);
            await service.PublishAsync(b.IdEvenementSession, idB);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.GetPublishedByCodeAsync("SAME-CODE"));

            var filtered = await service.GetPublishedByCodeAsync("SAME-CODE", idB);
            Assert.NotNull(filtered);
            Assert.Equal(idB, filtered!.IdSociete);
        }

        [Fact]
        public async Task GetPublishedByCodeAsync_returns_null_for_inactive_societe()
        {
            await using var ctx = BuildDb(nameof(GetPublishedByCodeAsync_returns_null_for_inactive_societe));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx, "Societe Inactive");
            var service = CreateService(ctx);

            var created = await service.CreateDraftAsync(BuildValidCreateRequest("INACTIVE-CODE", idSite), idSociete);
            await service.PublishAsync(created.IdEvenementSession, idSociete);

            var societe = await ctx.Societes.FirstAsync(s => s.IdSociete == idSociete);
            societe.Statut = false;
            await ctx.SaveChangesAsync();

            var byCode = await service.GetPublishedByCodeAsync("INACTIVE-CODE");
            var byCodeAndSociete = await service.GetPublishedByCodeAsync("INACTIVE-CODE", idSociete);

            Assert.Null(byCode);
            Assert.Null(byCodeAndSociete);
        }

        [Fact]
        public async Task GetByIdAsync_enriches_cover_price_availability_and_societe()
        {
            await using var ctx = BuildDb(nameof(GetByIdAsync_enriches_cover_price_availability_and_societe));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx, "Detail Co");
            var service = CreateService(ctx);

            var created = await service.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "DETAIL-ENRICH",
                IdSite = idSite,
                Libelle = "Détail enrichi",
                LogoOrganisateur = "https://cdn.example/detail.png",
                StartAtUtc = DateTime.UtcNow.AddDays(2),
                InventoryMode = "GlobalQuota",
                GlobalQuota = new EvenementCreateSessionGlobalQuotaDto
                {
                    CapaciteTotale = 40,
                    PrixUnitaire = 12m,
                    CodeDevise = "CDF"
                },
                Photos = new List<AddEvenementSessionPhotoDto>
                {
                    new()
                    {
                        PhotoBase64 = TinyJpegBase64(),
                        FileName = "cover.jpg",
                        Ordre = 1
                    },
                    new()
                    {
                        PhotoBase64 = TinyJpegBase64(),
                        FileName = "second.jpg",
                        Ordre = 2
                    }
                }
            }, idSociete);

            var detail = await service.GetByIdAsync(created.IdEvenementSession, idSociete);

            Assert.NotNull(detail);
            Assert.Equal("Detail Co", detail!.NomSociete);
            Assert.Equal("https://cdn.example/detail.png", detail.LogoOrganisateur);
            Assert.NotNull(detail.PhotoCouverture);
            Assert.Equal(1, detail.PhotoCouverture!.Ordre);
            Assert.Equal(2, detail.Photos.Count);
            Assert.Equal(12m, detail.PrixMin);
            Assert.Equal(12m, detail.PrixMax);
            Assert.Equal("CDF", detail.CodeDevise);
            Assert.Equal(40, detail.PlacesTotales);
            Assert.Equal(40, detail.PlacesRestantes);
            Assert.False(detail.IsSoldOut);
            Assert.NotNull(detail.GlobalQuota);
        }

        [Fact]
        public async Task GetByIdAsync_without_photo_has_null_cover_and_empty_photos()
        {
            await using var ctx = BuildDb(nameof(GetByIdAsync_without_photo_has_null_cover_and_empty_photos));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx, "No Pic Co");
            var service = CreateService(ctx);

            var draft = await service.CreateDraftAsync(BuildValidCreateRequest("NO-PIC", idSite), idSociete);
            var detail = await service.GetByIdAsync(draft.IdEvenementSession, idSociete);

            Assert.NotNull(detail);
            Assert.Null(detail!.PhotoCouverture);
            Assert.Empty(detail.Photos);
            Assert.Equal("No Pic Co", detail.NomSociete);
            Assert.Equal(10m, detail.PrixMin);
            Assert.Equal(50, detail.PlacesTotales);
            Assert.Equal(50, detail.PlacesRestantes);
            Assert.False(detail.IsSoldOut);
        }

        [Fact]
        public async Task ListAsync_enriches_cover_price_and_societe_name()
        {
            await using var ctx = BuildDb(nameof(ListAsync_enriches_cover_price_and_societe_name));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx, "Catalogue Co");
            var service = CreateService(ctx);

            var created = await service.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "ENRICH-GQ",
                IdSite = idSite,
                Libelle = "Session enrichie",
                StartAtUtc = DateTime.UtcNow.AddDays(3),
                InventoryMode = "GlobalQuota",
                GlobalQuota = new EvenementCreateSessionGlobalQuotaDto
                {
                    CapaciteTotale = 40,
                    PrixUnitaire = 25m,
                    CodeDevise = "USD"
                },
                Photos = new List<AddEvenementSessionPhotoDto>
                {
                    new()
                    {
                        PhotoBase64 = TinyJpegBase64(),
                        FileName = "cover.jpg",
                        Ordre = 1
                    }
                }
            }, idSociete);
            await service.PublishAsync(created.IdEvenementSession, idSociete);

            var list = await service.ListAsync(idSociete);
            var item = Assert.Single(list);

            Assert.Equal("Catalogue Co", item.NomSociete);
            Assert.NotNull(item.PhotoCouverture);
            Assert.False(string.IsNullOrWhiteSpace(item.PhotoCouverture!.PhotoUrl));
            Assert.True(string.IsNullOrEmpty(item.PhotoCouverture.PhotoBase64));
            Assert.Equal(1, item.PhotoCouverture.Ordre);
            Assert.Equal(25m, item.PrixMin);
            Assert.Equal(25m, item.PrixMax);
            Assert.Equal("USD", item.CodeDevise);
        }

        [Fact]
        public async Task ListAsync_class_quota_sets_prix_min_max()
        {
            await using var ctx = BuildDb(nameof(ListAsync_class_quota_sets_prix_min_max));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);
            var idVip = await SeedClasseAsync(ctx, idSociete, "VIP", "VIP");
            var idStd = await SeedClasseAsync(ctx, idSociete, "STD", "Standard");

            await service.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "ENRICH-CQ",
                IdSite = idSite,
                Libelle = "Classes",
                StartAtUtc = DateTime.UtcNow.AddDays(4),
                InventoryMode = "ClassQuota",
                ClassQuotas = new List<EvenementCreateSessionClassQuotaDto>
                {
                    new()
                    {
                        IdEvenementClasse = idVip,
                        CapaciteTotale = 10,
                        PrixUnitaire = 50m,
                        CodeDevise = "CDF"
                    },
                    new()
                    {
                        IdEvenementClasse = idStd,
                        CapaciteTotale = 30,
                        PrixUnitaire = 15m,
                        CodeDevise = "CDF"
                    }
                }
            }, idSociete);

            var list = await service.ListAsync(idSociete);
            var item = Assert.Single(list);

            Assert.Equal(15m, item.PrixMin);
            Assert.Equal(50m, item.PrixMax);
            Assert.Equal("CDF", item.CodeDevise);
            Assert.Null(item.PhotoCouverture);
        }

        [Fact]
        public async Task ListPublishedGlobalAsync_without_photo_has_null_cover()
        {
            await using var ctx = BuildDb(nameof(ListPublishedGlobalAsync_without_photo_has_null_cover));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx, "No Photo Co");
            var service = CreateService(ctx);

            var draft = await service.CreateDraftAsync(BuildValidCreateRequest("NO-COVER", idSite), idSociete);
            await service.PublishAsync(draft.IdEvenementSession, idSociete);

            var list = await service.ListPublishedGlobalAsync();
            var item = Assert.Single(list);

            Assert.Null(item.PhotoCouverture);
            Assert.Equal("No Photo Co", item.NomSociete);
            Assert.Equal(10m, item.PrixMin);
            Assert.Equal("CDF", item.CodeDevise);
        }

        private static string TinyJpegBase64() =>
            Convert.ToBase64String(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 });

        private static EvenementCreateSessionRequestDto BuildValidCreateRequest(string code, int idSite) => new()
        {
            CodeSession = code,
            IdSite = idSite,
            Libelle = "Test session",
            StartAtUtc = DateTime.UtcNow.AddDays(5),
            InventoryMode = "GlobalQuota",
            GlobalQuota = new EvenementCreateSessionGlobalQuotaDto
            {
                CapaciteTotale = 50,
                PrixUnitaire = 10m,
                CodeDevise = "CDF"
            }
        };

        private static async Task<(int IdSociete, int IdSite)> SeedSocieteAsync(
            CongoTravelDbContext ctx,
            string nom = "Test Societe") =>
            await EvenementTestFactories.SeedSocieteWithSiteAsync(ctx, nom);

        private static async Task<int> SeedClasseAsync(
            CongoTravelDbContext ctx,
            int idSociete,
            string code = "VIP",
            string libelle = "VIP")
        {
            var classe = new EvenementClasse
            {
                IdSociete = idSociete,
                CodeClasse = code,
                Libelle = libelle,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementClasses.Add(classe);
            await ctx.SaveChangesAsync();
            return classe.IdEvenementClasse;
        }
    }
}
