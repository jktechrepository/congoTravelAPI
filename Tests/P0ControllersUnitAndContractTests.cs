using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Controllers;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Client;
using CongoTravel.Models.DTOs.Reservation;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using System.Reflection;
using Xunit;

namespace CongoTravel.Tests
{
    public class P0ControllersUnitAndContractTests
    {
        private static CongoTravelDbContext BuildDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(dbName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new CongoTravelDbContext(options);
        }

        private static CongoTravelDbContext CreateVoyageControllerDbContext() =>
            BuildDbContext($"voyage-controller-{Guid.NewGuid():N}");

        private static void SetupVoyageRepoRepartitionEmpty(Mock<IVoyageRepository> voyageRepo)
        {
            voyageRepo
                .Setup(r => r.GetRepartitionSiegesDisponiblesParVoyagesAsync(It.IsAny<IReadOnlyList<int>>()))
                .ReturnsAsync(new Dictionary<int, List<VoyageCategorieSiegeDisponiblesSummaryDto>>());
        }

        [Fact]
        public void AuthTest_PublicEndpoint_returns_ok()
        {
            var currentUser = new Mock<ICurrentUserService>();
            var controller = new AuthTestController(currentUser.Object, NullLogger<AuthTestController>.Instance);

            var result = controller.PublicEndpoint();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public void AuthTest_ProtectedEndpoint_returns_ok_when_current_user_available()
        {
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
            currentUser.SetupGet(x => x.UserId).Returns(42);
            currentUser.SetupGet(x => x.UserName).Returns("tester");
            currentUser.SetupGet(x => x.UserRole).Returns("Admin");
            currentUser.SetupGet(x => x.SocieteId).Returns(1);
            currentUser.SetupGet(x => x.IsSuperAdmin).Returns(false);
            currentUser.SetupGet(x => x.IsAdmin).Returns(true);

            var controller = new AuthTestController(currentUser.Object, NullLogger<AuthTestController>.Instance);

            var result = controller.ProtectedEndpoint();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Client_GetClients_returns_paged_result()
        {
            await using var db = BuildDbContext(nameof(Client_GetClients_returns_paged_result));
            var repo = new Mock<IClientRepository>();
            repo.Setup(r => r.GetPagedAsync(It.IsAny<ClientPagedSearchRequestDto>()))
                .ReturnsAsync(new PagedResult<Client>(
                    new List<Client>
                    {
                        new()
                        {
                            IdClient = 1,
                            NomClient = "Jean",
                            AdresseClient = "Kin",
                            EmailClient = "jean@test.com",
                            Telephone = "099",
                            Statut = true,
                            IsActif = true
                        }
                    },
                    1,
                    1,
                    20));

            var controller = new ClientController(
                new Mock<IAuditService>().Object,
                new Mock<ICurrentUserService>().Object,
                null!,
                repo.Object,
                db,
                NullLogger<ClientController>.Instance,
                new Mock<IEmailVerificationService>().Object);

            var action = await controller.GetClients(new ClientPagedSearchRequestDto { PageNumber = 1, PageSize = 20 });
            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var payload = Assert.IsType<PagedResult<ClientResponseDto>>(ok.Value);

            Assert.Equal(1, payload.TotalCount);
            Assert.Single(payload.Data);
        }

        [Fact]
        public async Task Voyage_GetById_returns_not_found_when_missing()
        {
            var voyageRepo = new Mock<IVoyageRepository>();
            SetupVoyageRepoRepartitionEmpty(voyageRepo);
            voyageRepo.Setup(r => r.GetByIdPublicAsync(123)).ReturnsAsync((Voyage?)null);
            var tarifService = new Mock<IVoyageTarifService>();
            var mapper = new Mock<IMapper>();
            var reportService = new Mock<IVoyageReportService>();
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.SocieteId).Returns(1);
            currentUser.SetupGet(x => x.UserId).Returns(1);
            currentUser.SetupGet(x => x.UserName).Returns("tester");
            var controller = new VoyageController(
                voyageRepo.Object,
                tarifService.Object,
                reportService.Object,
                currentUser.Object,
                CreateVoyageControllerDbContext(),
                mapper.Object,
                NullLogger<VoyageController>.Instance);

            var action = await controller.GetById(123);

            Assert.IsType<NotFoundObjectResult>(action.Result);
        }

        [Fact]
        public async Task Voyage_GetById_returns_tarifs_in_response()
        {
            var voyageRepo = new Mock<IVoyageRepository>();
            SetupVoyageRepoRepartitionEmpty(voyageRepo);
            voyageRepo.Setup(r => r.GetByIdPublicAsync(124)).ReturnsAsync(new Voyage
            {
                Id = 124,
                Prix = 1000,
                CodeDevisePrix = "CDF",
                VoyageTarifsCategorieSiege = new List<VoyageTarifCategorieSiege>
                {
                    new() { IdCategorieSiege = 1, Prix = 1000, CategorieSiege = new CategorieSiege { CodeCategorieSiege = "ECO", Libelle = "Eco" } },
                    new() { IdCategorieSiege = 2, Prix = 1500, CategorieSiege = new CategorieSiege { CodeCategorieSiege = "VIP", Libelle = "Vip" } }
                }
            });
            var tarifService = new Mock<IVoyageTarifService>();
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<VoyageResponseDto>(It.IsAny<Voyage>())).Returns<Voyage>(v => new VoyageResponseDto
            {
                Id = v.Id,
                Prix = v.Prix,
                CodeDevisePrix = v.CodeDevisePrix
            });

            var reportService = new Mock<IVoyageReportService>();
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.SocieteId).Returns(1);
            currentUser.SetupGet(x => x.UserId).Returns(1);
            currentUser.SetupGet(x => x.UserName).Returns("tester");
            var controller = new VoyageController(
                voyageRepo.Object,
                tarifService.Object,
                reportService.Object,
                currentUser.Object,
                CreateVoyageControllerDbContext(),
                mapper.Object,
                NullLogger<VoyageController>.Instance);
            var action = await controller.GetById(124);

            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var payload = Assert.IsType<VoyageResponseDto>(ok.Value);
            Assert.NotNull(payload.Tarifs);
            Assert.Equal(2, payload.Tarifs!.Count);
            Assert.Contains(payload.Tarifs, t => t.IdCategorieSiege == 1 && t.Prix == 1000);
            Assert.Contains(payload.Tarifs, t => t.IdCategorieSiege == 2 && t.Prix == 1500);
            Assert.Contains(payload.Tarifs, t => t.IdCategorieSiege == 1 && t.Libelle == "ECO");
        }

        [Fact]
        public async Task Voyage_GetTarifsCategorieSiege_returns_not_found_when_public_voyage_is_hidden()
        {
            var voyageRepo = new Mock<IVoyageRepository>();
            SetupVoyageRepoRepartitionEmpty(voyageRepo);
            voyageRepo.Setup(r => r.GetByIdPublicAsync(125)).ReturnsAsync((Voyage?)null);
            var tarifService = new Mock<IVoyageTarifService>();
            var mapper = new Mock<IMapper>();
            var reportService = new Mock<IVoyageReportService>();
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.SocieteId).Returns(1);
            currentUser.SetupGet(x => x.UserId).Returns(1);
            currentUser.SetupGet(x => x.UserName).Returns("tester");
            var controller = new VoyageController(
                voyageRepo.Object,
                tarifService.Object,
                reportService.Object,
                currentUser.Object,
                CreateVoyageControllerDbContext(),
                mapper.Object,
                NullLogger<VoyageController>.Instance);

            var action = await controller.GetTarifsCategorieSiege(125);

            Assert.IsType<NotFoundObjectResult>(action.Result);
            tarifService.Verify(t => t.GetTarifsByVoyageAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Voyage_Create_applies_initial_tarifs_when_provided()
        {
            var voyageRepo = new Mock<IVoyageRepository>();
            SetupVoyageRepoRepartitionEmpty(voyageRepo);
            voyageRepo
                .Setup(r => r.CreateAsync(It.IsAny<Voyage>(), It.IsAny<IReadOnlyList<CreateVoyageEtapeDto>?>()))
                .ReturnsAsync((Voyage v, IReadOnlyList<CreateVoyageEtapeDto>? _) =>
                {
                    v.Id = 77;
                    return v;
                });
            voyageRepo.Setup(r => r.GetByIdAsync(77)).ReturnsAsync(new Voyage
            {
                Id = 77,
                IdSociete = 3,
                IdVehicule = 5,
                IdDestination = 6,
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 1000,
                CodeDevisePrix = "CDF",
                VoyageTarifsCategorieSiege = new List<VoyageTarifCategorieSiege>
                {
                    new() { IdCategorieSiege = 10, Prix = 900 },
                    new() { IdCategorieSiege = 11, Prix = 1200 }
                }
            });

            var tarifService = new Mock<IVoyageTarifService>();
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<Voyage>(It.IsAny<CreateVoyageDto>())).Returns<CreateVoyageDto>(dto => new Voyage
            {
                IdSociete = dto.IdSociete,
                IdVehicule = dto.IdVehicule,
                IdSite = dto.IdSite,
                DateDepart = dto.DateDepart,
                HeureDepart = dto.HeureDepart,
                Prix = dto.Prix,
                CodeDevisePrix = dto.CodeDevisePrix
            });
            mapper.Setup(m => m.Map<VoyageResponseDto>(It.IsAny<Voyage>())).Returns<Voyage>(v => new VoyageResponseDto
            {
                Id = v.Id
            });

            var reportService = new Mock<IVoyageReportService>();
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.SocieteId).Returns(1);
            currentUser.SetupGet(x => x.UserId).Returns(1);
            currentUser.SetupGet(x => x.UserName).Returns("tester");
            var controller = new VoyageController(
                voyageRepo.Object,
                tarifService.Object,
                reportService.Object,
                currentUser.Object,
                CreateVoyageControllerDbContext(),
                mapper.Object,
                NullLogger<VoyageController>.Instance);
            var dto = new CreateVoyageDto
            {
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 1000,
                CodeDevisePrix = "CDF",
                IdVehicule = 5,
                IdDestination = 6,
                IdSociete = 3,
                IdSite = 2,
                Tarifs = new List<Models.DTOs.VoyageTarification.VoyageTarifCategorieSiegeItemDto>
                {
                    new() { IdCategorieSiege = 10, Prix = 900 },
                    new() { IdCategorieSiege = 11, Prix = 1200 }
                }
            };

            var action = await controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(action.Result);
            var payload = Assert.IsType<VoyageResponseDto>(created.Value);
            Assert.NotNull(payload.Tarifs);
            Assert.Equal(2, payload.Tarifs!.Count);
            tarifService.Verify(
                s => s.ReplaceTarifsForVoyageAsync(
                    77,
                    3,
                    It.Is<IReadOnlyList<(int IdCategorieSiege, int Prix)>>(l =>
                        l.Count == 2
                        && l.Any(x => x.IdCategorieSiege == 10 && x.Prix == 900)
                        && l.Any(x => x.IdCategorieSiege == 11 && x.Prix == 1200)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Voyage_Create_keeps_default_tarif_when_tarifs_missing()
        {
            var voyageRepo = new Mock<IVoyageRepository>();
            SetupVoyageRepoRepartitionEmpty(voyageRepo);
            voyageRepo
                .Setup(r => r.CreateAsync(It.IsAny<Voyage>(), It.IsAny<IReadOnlyList<CreateVoyageEtapeDto>?>()))
                .ReturnsAsync((Voyage v, IReadOnlyList<CreateVoyageEtapeDto>? _) =>
                {
                    v.Id = 78;
                    return v;
                });
            voyageRepo.Setup(r => r.GetByIdAsync(78)).ReturnsAsync(new Voyage
            {
                Id = 78,
                IdSociete = 3,
                IdVehicule = 5,
                IdDestination = 6,
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 1000,
                CodeDevisePrix = "CDF"
            });

            var tarifService = new Mock<IVoyageTarifService>();
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<Voyage>(It.IsAny<CreateVoyageDto>())).Returns<CreateVoyageDto>(dto => new Voyage
            {
                IdSociete = dto.IdSociete,
                IdVehicule = dto.IdVehicule,
                IdSite = dto.IdSite,
                DateDepart = dto.DateDepart,
                HeureDepart = dto.HeureDepart,
                Prix = dto.Prix,
                CodeDevisePrix = dto.CodeDevisePrix
            });
            mapper.Setup(m => m.Map<VoyageResponseDto>(It.IsAny<Voyage>())).Returns<Voyage>(v => new VoyageResponseDto
            {
                Id = v.Id
            });

            var reportService = new Mock<IVoyageReportService>();
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.SocieteId).Returns(1);
            currentUser.SetupGet(x => x.UserId).Returns(1);
            currentUser.SetupGet(x => x.UserName).Returns("tester");
            var controller = new VoyageController(
                voyageRepo.Object,
                tarifService.Object,
                reportService.Object,
                currentUser.Object,
                CreateVoyageControllerDbContext(),
                mapper.Object,
                NullLogger<VoyageController>.Instance);
            var dto = new CreateVoyageDto
            {
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 1000,
                CodeDevisePrix = "CDF",
                IdVehicule = 5,
                IdDestination = 6,
                IdSociete = 3,
                IdSite = 2
            };

            var action = await controller.Create(dto);

            Assert.IsType<CreatedAtActionResult>(action.Result);
            tarifService.Verify(
                s => s.ReplaceTarifsForVoyageAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<IReadOnlyList<(int IdCategorieSiege, int Prix)>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Voyage_Update_applies_tarifs_when_provided()
        {
            var voyageRepo = new Mock<IVoyageRepository>();
            SetupVoyageRepoRepartitionEmpty(voyageRepo);
            voyageRepo
                .Setup(r => r.EnsurePrixUpdateAllowedAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            voyageRepo
                .Setup(r => r.UpdateAsync(It.IsAny<Voyage>(), It.IsAny<IReadOnlyList<CreateVoyageEtapeDto>?>()))
                .ReturnsAsync((Voyage v, IReadOnlyList<CreateVoyageEtapeDto>? _) => v);
            voyageRepo.Setup(r => r.GetByIdAsync(90)).ReturnsAsync(new Voyage
            {
                Id = 90,
                IdSociete = 3,
                IdVehicule = 5,
                IdDestination = 6,
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 1000,
                CodeDevisePrix = "CDF",
                VoyageTarifsCategorieSiege = new List<VoyageTarifCategorieSiege>
                {
                    new() { IdCategorieSiege = 10, Prix = 900 },
                    new() { IdCategorieSiege = 11, Prix = 1200 }
                }
            });

            var tarifService = new Mock<IVoyageTarifService>();
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<Voyage>(It.IsAny<UpdateVoyageDto>())).Returns<UpdateVoyageDto>(dto => new Voyage
            {
                Id = dto.Id,
                IdSociete = dto.IdSociete,
                IdVehicule = dto.IdVehicule,
                IdSite = dto.IdSite,
                DateDepart = dto.DateDepart,
                HeureDepart = dto.HeureDepart,
                Prix = dto.Prix,
                CodeDevisePrix = dto.CodeDevisePrix
            });
            mapper.Setup(m => m.Map<VoyageResponseDto>(It.IsAny<Voyage>())).Returns<Voyage>(v => new VoyageResponseDto
            {
                Id = v.Id
            });

            var reportService = new Mock<IVoyageReportService>();
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.SocieteId).Returns(1);
            currentUser.SetupGet(x => x.UserId).Returns(1);
            currentUser.SetupGet(x => x.UserName).Returns("tester");
            var controller = new VoyageController(
                voyageRepo.Object,
                tarifService.Object,
                reportService.Object,
                currentUser.Object,
                CreateVoyageControllerDbContext(),
                mapper.Object,
                NullLogger<VoyageController>.Instance);
            var dto = new UpdateVoyageDto
            {
                Id = 90,
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 1000,
                CodeDevisePrix = "CDF",
                IdVehicule = 5,
                IdDestination = 6,
                IdSociete = 3,
                IdSite = 2,
                Statut = true,
                Tarifs = new List<Models.DTOs.VoyageTarification.VoyageTarifCategorieSiegeItemDto>
                {
                    new() { IdCategorieSiege = 10, Prix = 900 },
                    new() { IdCategorieSiege = 11, Prix = 1200 }
                }
            };

            var action = await controller.Update(90, dto);

            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var payload = Assert.IsType<VoyageResponseDto>(ok.Value);
            Assert.NotNull(payload.Tarifs);
            Assert.Equal(2, payload.Tarifs!.Count);
            tarifService.Verify(
                s => s.ReplaceTarifsForVoyageAsync(
                    90,
                    3,
                    It.Is<IReadOnlyList<(int IdCategorieSiege, int Prix)>>(l =>
                        l.Count == 2
                        && l.Any(x => x.IdCategorieSiege == 10 && x.Prix == 900)
                        && l.Any(x => x.IdCategorieSiege == 11 && x.Prix == 1200)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Voyage_Update_does_not_replace_tarifs_when_missing()
        {
            var voyageRepo = new Mock<IVoyageRepository>();
            SetupVoyageRepoRepartitionEmpty(voyageRepo);
            voyageRepo
                .Setup(r => r.EnsurePrixUpdateAllowedAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            voyageRepo
                .Setup(r => r.UpdateAsync(It.IsAny<Voyage>(), It.IsAny<IReadOnlyList<CreateVoyageEtapeDto>?>()))
                .ReturnsAsync((Voyage v, IReadOnlyList<CreateVoyageEtapeDto>? _) => v);
            voyageRepo.Setup(r => r.GetByIdAsync(91)).ReturnsAsync(new Voyage
            {
                Id = 91,
                IdSociete = 3,
                IdVehicule = 5,
                IdDestination = 6,
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 1000,
                CodeDevisePrix = "CDF"
            });

            var tarifService = new Mock<IVoyageTarifService>();
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<Voyage>(It.IsAny<UpdateVoyageDto>())).Returns<UpdateVoyageDto>(dto => new Voyage
            {
                Id = dto.Id,
                IdSociete = dto.IdSociete,
                IdVehicule = dto.IdVehicule,
                IdSite = dto.IdSite,
                DateDepart = dto.DateDepart,
                HeureDepart = dto.HeureDepart,
                Prix = dto.Prix,
                CodeDevisePrix = dto.CodeDevisePrix
            });
            mapper.Setup(m => m.Map<VoyageResponseDto>(It.IsAny<Voyage>())).Returns<Voyage>(v => new VoyageResponseDto
            {
                Id = v.Id
            });

            var reportService = new Mock<IVoyageReportService>();
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.SocieteId).Returns(1);
            currentUser.SetupGet(x => x.UserId).Returns(1);
            currentUser.SetupGet(x => x.UserName).Returns("tester");
            var controller = new VoyageController(
                voyageRepo.Object,
                tarifService.Object,
                reportService.Object,
                currentUser.Object,
                CreateVoyageControllerDbContext(),
                mapper.Object,
                NullLogger<VoyageController>.Instance);
            var dto = new UpdateVoyageDto
            {
                Id = 91,
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 1000,
                CodeDevisePrix = "CDF",
                IdVehicule = 5,
                IdDestination = 6,
                IdSociete = 3,
                IdSite = 2,
                Statut = true
            };

            var action = await controller.Update(91, dto);

            Assert.IsType<OkObjectResult>(action.Result);
            tarifService.Verify(
                s => s.ReplaceTarifsForVoyageAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<IReadOnlyList<(int IdCategorieSiege, int Prix)>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Voyage_Update_returns_bad_request_when_prix_changes_without_tarifs()
        {
            var voyageRepo = new Mock<IVoyageRepository>();
            SetupVoyageRepoRepartitionEmpty(voyageRepo);
            voyageRepo
                .Setup(r => r.EnsurePrixUpdateAllowedAsync(92, 2000, false, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException(
                    "Pour modifier le prix, précisez la catégorie de siège via tarifs[]."));

            var tarifService = new Mock<IVoyageTarifService>();
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<Voyage>(It.IsAny<UpdateVoyageDto>())).Returns<UpdateVoyageDto>(dto => new Voyage
            {
                Id = dto.Id,
                IdSociete = dto.IdSociete,
                IdVehicule = dto.IdVehicule,
                IdSite = dto.IdSite,
                DateDepart = dto.DateDepart,
                HeureDepart = dto.HeureDepart,
                Prix = dto.Prix,
                CodeDevisePrix = dto.CodeDevisePrix
            });

            var reportService = new Mock<IVoyageReportService>();
            var currentUser = new Mock<ICurrentUserService>();
            var controller = new VoyageController(
                voyageRepo.Object,
                tarifService.Object,
                reportService.Object,
                currentUser.Object,
                CreateVoyageControllerDbContext(),
                mapper.Object,
                NullLogger<VoyageController>.Instance);

            var dto = new UpdateVoyageDto
            {
                Id = 92,
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 2000,
                CodeDevisePrix = "CDF",
                IdVehicule = 5,
                IdDestination = 6,
                IdSociete = 3,
                IdSite = 2,
                Statut = true
            };

            var action = await controller.Update(92, dto);

            var bad = Assert.IsType<BadRequestObjectResult>(action.Result);
            Assert.NotNull(bad.Value);
            voyageRepo.Verify(r => r.UpdateAsync(It.IsAny<Voyage>(), It.IsAny<IReadOnlyList<CreateVoyageEtapeDto>?>()), Times.Never);
        }

        [Fact]
        public async Task Reservation_GetById_returns_not_found_when_missing()
        {
            var reservationRepo = new Mock<IReservationRepository>();
            reservationRepo.Setup(r => r.GetByIdAsync(321)).ReturnsAsync((Reservation?)null);
            var billetRepo = new Mock<IBilletRepository>();
            var mapper = new Mock<IMapper>();
            await using var db = BuildDbContext(nameof(Reservation_GetById_returns_not_found_when_missing));
            var readService = new Mock<IReservationWithPaiementReadService>();
            readService
                .Setup(r => r.BuildByReservationIdAsync(321, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ReservationWithPaiementResponseDto?)null);
            var controller = new ReservationController(
                reservationRepo.Object,
                billetRepo.Object,
                mapper.Object,
                NullLogger<ReservationController>.Instance,
                new Mock<ICashReservationWithPaiementService>().Object,
                new Mock<IFlexPayReservationService>().Object,
                readService.Object,
                new Mock<IBilletPricingEnrichmentService>().Object,
                db,
                Mock.Of<ICurrentUserService>());

            var action = await controller.GetById(321);

            Assert.IsType<NotFoundObjectResult>(action.Result);
        }

        [Fact]
        public async Task Paiement_GetById_returns_not_found_when_missing()
        {
            await using var db = BuildDbContext(nameof(Paiement_GetById_returns_not_found_when_missing));
            var repo = new Mock<IPaiementRepository>();
            repo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Paiement?)null);
            var controller = new PaiementController(
                repo.Object,
                NullLogger<PaiementController>.Instance,
                db,
                Mock.Of<ICurrentUserService>());

            var result = await controller.GetById(404);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Billet_CheckBillet_returns_ok()
        {
            var repo = new Mock<IBilletRepository>();
            repo.Setup(r => r.CheckBilletByQrCodeAsync("QR-TEST-5", null))
                .ReturnsAsync(new BilletCheckResponseDto
                {
                    IdBillet = 5,
                    IsUsed = false,
                    Statut = "Valide",
                    EmbarquementAutorise = true,
                    Message = "Billet valide"
                });
            var mapper = new Mock<IMapper>();
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(c => c.SocieteId).Returns(1);
            currentUser.Setup(c => c.IsSuperAdmin).Returns(false);
            var controller = new BilletController(
                repo.Object,
                new Mock<IBilletPricingEnrichmentService>().Object,
                new Mock<IBilletReportService>().Object,
                mapper.Object,
                NullLogger<BilletController>.Instance,
                currentUser.Object);

            var action = await controller.CheckBillet("QR-TEST-5");

            var ok = Assert.IsType<OkObjectResult>(action.Result);
            Assert.IsType<BilletCheckResponseDto>(ok.Value);
        }

        [Fact]
        public async Task Billet_GetByQrCode_returns_passenger_identity_in_nomClient_fields()
        {
            var passenger = new ReservationPassenger
            {
                NomComplet = "Passager Réel",
                Telephone = "+243999"
            };
            var client = new Client
            {
                NomClient = "Acheteur Dupont",
                Telephone = "+243111"
            };
            var billet = new Billet
            {
                IdBillet = 42,
                QrCode = "QR-QRCODE-ROUTE",
                Reservation = new Reservation { Client = client },
                ReservationPassenger = passenger
            };

            var repo = new Mock<IBilletRepository>();
            repo.Setup(r => r.GetByQrCodeAsync("QR-QRCODE-ROUTE"))
                .ReturnsAsync(new[] { billet });

            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<List<BilletResponseDto>>(It.IsAny<List<Billet>>()))
                .Returns((List<Billet> src) => src.Select(b => new BilletResponseDto
                {
                    IdBillet = b.IdBillet,
                    QrCode = b.QrCode,
                    NomClient = b.Reservation?.Client?.NomClient,
                    TelephoneClient = b.Reservation?.Client?.Telephone
                }).ToList());

            var enrichment = new Mock<IBilletPricingEnrichmentService>();
            enrichment.Setup(e => e.EnrichPrixVoyageAsync(
                    It.IsAny<IReadOnlyList<Billet>>(),
                    It.IsAny<IList<BilletResponseDto>>()))
                .Returns(Task.CompletedTask);

            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(c => c.SocieteId).Returns(1);
            currentUser.Setup(c => c.IsSuperAdmin).Returns(false);
            var controller = new BilletController(
                repo.Object,
                enrichment.Object,
                new Mock<IBilletReportService>().Object,
                mapper.Object,
                NullLogger<BilletController>.Instance,
                currentUser.Object);

            var action = await controller.GetByQrCode("QR-QRCODE-ROUTE");

            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<BilletResponseDto>>(ok.Value).ToList();
            Assert.Single(list);
            Assert.Equal("Passager Réel", list[0].NomClient);
            Assert.Equal("+243999", list[0].TelephoneClient);
            Assert.NotEqual(client.NomClient, list[0].NomClient);
        }

        [Fact]
        public async Task Billet_GetById_returns_not_found_when_missing()
        {
            var repo = new Mock<IBilletRepository>();
            repo.Setup(r => r.GetByIdAsync(777)).ReturnsAsync((Billet?)null);
            var mapper = new Mock<IMapper>();
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(c => c.SocieteId).Returns(1);
            currentUser.Setup(c => c.IsSuperAdmin).Returns(false);
            var controller = new BilletController(
                repo.Object,
                new Mock<IBilletPricingEnrichmentService>().Object,
                new Mock<IBilletReportService>().Object,
                mapper.Object,
                NullLogger<BilletController>.Instance,
                currentUser.Object);

            var action = await controller.GetById(777);

            Assert.IsType<NotFoundObjectResult>(action.Result);
        }

        [Fact]
        public async Task Dashboard_GetDashboardStats_returns_forbidden_when_token_societe_mismatch()
        {
            await using var db = BuildDbContext(nameof(Dashboard_GetDashboardStats_returns_forbidden_when_token_societe_mismatch));
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.SocieteId).Returns(2);
            currentUser.SetupGet(x => x.UserId).Returns(77);
            var service = DashboardEnrichmentTestHelper.CreateTransportDashboardService(db, currentUser.Object);

            var controller = new DashboardController(service, currentUser.Object, NullLogger<DashboardController>.Instance);

            var action = await controller.GetDashboardStats(1);

            var forbidden = Assert.IsType<ObjectResult>(action.Result);
            Assert.Equal(403, forbidden.StatusCode);
        }

        [Fact]
        public async Task Dashboard_GetDashboardStats_returns_ok_and_real_metrics_when_token_matches()
        {
            await using var db = BuildDbContext(nameof(Dashboard_GetDashboardStats_returns_ok_and_real_metrics_when_token_matches));

            db.Agents.Add(new Agent
            {
                IdAgent = 1,
                IdSociete = 1,
                NomComplet = "Agent A",
                Matricule = "AG-001",
                DateNaissance = new DateTime(1990, 1, 1),
                Statut = true
            });

            db.Utilisateurs.Add(new Utilisateur
            {
                IdUtilisateur = 10,
                IdSociete = 1,
                IdAgent = 1,
                NomComplet = "User Agent A",
                MotDePasseHash = "hash",
                Statut = true
            });

            db.Clients.Add(new Client
            {
                IdClient = 100,
                NomClient = "Client A",
                AdresseClient = "Kin",
                Statut = true,
                IsActif = true,
                IsDeleted = false
            });

            db.Voyages.Add(new Voyage
            {
                Id = 1000,
                IdSociete = 1,
                IdVehicule = 1,
                IdDestination = 1,
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 5000,
                PrixDevisePrincipale = 5000,
                CodeDevisePrix = "CDF",
                CodeDevisePrincipale = "CDF",
                Statut = true
            });

            db.Reservations.Add(new Reservation
            {
                IdReservation = 500,
                IdSociete = 1,
                IdClient = 100,
                IdUtilisateur = 10,
                IdVoyage = 1000,
                DateReservation = DateTime.UtcNow.Date,
                StatutReservation = "CONFIRMEE",
                Statut = true,
                NombreDePlace = 1
            });

            db.Paiements.Add(new Paiement
            {
                IdPaiement = 900,
                IdSociete = 1,
                IdReservation = 500,
                IdUtilisateur = 10,
                MontantAPaye = 5000,
                MontantPaye = 5000,
                MontantAPayeDevisePrincipale = 5000,
                MontantPayeDevisePrincipale = 5000,
                CodeDevisePaiement = "CDF",
                CodeDevisePrincipale = "CDF",
                DatePaiement = DateTime.UtcNow,
                Statut = true,
                IsDeleted = false
            });

            await db.SaveChangesAsync();

            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.SocieteId).Returns(1);
            currentUser.SetupGet(x => x.UserId).Returns(10);
            var service = DashboardEnrichmentTestHelper.CreateTransportDashboardService(db, currentUser.Object);

            var controller = new DashboardController(service, currentUser.Object, NullLogger<DashboardController>.Instance);

            var action = await controller.GetDashboardStats(1);

            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var payload = Assert.IsType<DashboardDto>(ok.Value);
            Assert.Equal(1, payload.TotalAgents);
            Assert.Equal(1, payload.TotalClientsActifs);
            Assert.True(payload.CollecteMois.Montant > 0);
            Assert.Equal(1, payload.TransportStatistiques.VoyagesActifs);
            Assert.NotNull(payload.Top5AgentsCollecteurs);
            Assert.Single(payload.Top5AgentsCollecteurs);
            Assert.Equal(1, payload.Top5AgentsCollecteurs[0].IdAgent);
        }

        [Theory]
        [InlineData(typeof(ClientController), 15)]
        [InlineData(typeof(VoyageController), 38)]
        [InlineData(typeof(ReservationController), 36)]
        [InlineData(typeof(PaiementController), 9)]
        [InlineData(typeof(BilletController), 16)]
        [InlineData(typeof(FeuilleDeRouteController), 4)]
        [InlineData(typeof(EvenementClasseController), 7)]
        [InlineData(typeof(EvenementSessionController), 15)]
        [InlineData(typeof(EvenementReservationController), 13)]
        [InlineData(typeof(EvenementTicketController), 12)]
        [InlineData(typeof(AuthTestController), 3)]
        public void P0_endpoint_contract_http_attribute_count_is_stable(Type controllerType, int expectedHttpEndpoints)
        {
            var methods = controllerType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            var count = methods.Count(m => m
                .GetCustomAttributes(inherit: true)
                .Any(a => a is HttpMethodAttribute));

            Assert.Equal(expectedHttpEndpoints, count);
        }
    }
}
