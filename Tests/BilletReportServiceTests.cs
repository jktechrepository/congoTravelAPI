using CongoTravel.Models;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CongoTravel.Tests
{
    public class BilletReportServiceTests
    {
        [Fact]
        public async Task GeneratePdfAsync_returns_not_found_when_billet_missing()
        {
            var repo = new Mock<IBilletRepository>();
            repo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Billet?)null);

            var svc = CreateService(repo.Object);
            var result = await svc.GeneratePdfAsync(999);

            Assert.Equal(BilletPdfOutcomeStatus.NotFound, result.Status);
            Assert.Null(result.Pdf);
        }

        [Fact]
        public async Task GeneratePdfAsync_returns_not_aerial_for_terrestrial_vehicle()
        {
            var billet = BuildBillet(typeLibelle: "Terrestre");
            var repo = new Mock<IBilletRepository>();
            repo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(billet);

            var svc = CreateService(repo.Object);
            var result = await svc.GeneratePdfAsync(42);

            Assert.Equal(BilletPdfOutcomeStatus.NotAerial, result.Status);
            Assert.Null(result.Pdf);
            Assert.Contains("aérien", result.Message!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GeneratePdfAsync_produces_pdf_bytes_for_aerial_billet()
        {
            var reportPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Reports", "Billet_A4.frx"));
            Assert.True(File.Exists(reportPath), $"Template manquant: {reportPath}");

            var billet = BuildBillet(typeLibelle: "Aérien");
            var repo = new Mock<IBilletRepository>();
            repo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(billet);

            var configRepo = new Mock<IConfigSocieteRepository>();
            configRepo.Setup(c => c.GetBySocieteAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ConfigSociete { IdSociete = 1, PoidsBagageParKiloOffert = 20m });

            var env = new Mock<IWebHostEnvironment>();
            env.SetupGet(e => e.ContentRootPath).Returns(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")));

            var svc = new BilletReportService(
                repo.Object,
                configRepo.Object,
                env.Object,
                NullLogger<BilletReportService>.Instance);

            var result = await svc.GeneratePdfAsync(42);

            Assert.Equal(BilletPdfOutcomeStatus.Success, result.Status);
            Assert.NotNull(result.Pdf);
            Assert.Equal("billet_d_avion_a4-42.pdf", result.Pdf!.FileName);
            Assert.Equal("application/pdf", result.Pdf.ContentType);
            Assert.True(result.Pdf.Content.Length > 1000, "PDF trop petit");
            Assert.Equal(0x25, result.Pdf.Content[0]);
            Assert.Equal((byte)'P', result.Pdf.Content[1]);
            Assert.Equal((byte)'D', result.Pdf.Content[2]);
            Assert.Equal((byte)'F', result.Pdf.Content[3]);
        }

        [Fact]
        public async Task GenerateHtmlPreviewAsync_produces_html_for_aerial_billet()
        {
            var reportPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Reports", "Billet_A4.frx"));
            Assert.True(File.Exists(reportPath), $"Template manquant: {reportPath}");

            var billet = BuildBillet(typeLibelle: "Aérien");
            var repo = new Mock<IBilletRepository>();
            repo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(billet);

            var configRepo = new Mock<IConfigSocieteRepository>();
            configRepo.Setup(c => c.GetBySocieteAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ConfigSociete { IdSociete = 1, PoidsBagageParKiloOffert = 20m });

            var env = new Mock<IWebHostEnvironment>();
            env.SetupGet(e => e.ContentRootPath).Returns(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")));

            var svc = new BilletReportService(
                repo.Object,
                configRepo.Object,
                env.Object,
                NullLogger<BilletReportService>.Instance);

            var result = await svc.GenerateHtmlPreviewAsync(42);

            Assert.Equal(BilletPdfOutcomeStatus.Success, result.Status);
            Assert.NotNull(result.Pdf);
            Assert.Equal("billet_d_avion_a4-42.html", result.Pdf!.FileName);
            Assert.StartsWith("text/html", result.Pdf.ContentType);
            var html = System.Text.Encoding.UTF8.GetString(result.Pdf.Content);
            Assert.Contains("<", html);
            Assert.DoesNotContain("FastReport", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<title>", html, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("Aérien", true)]
        [InlineData("aerien", true)]
        [InlineData("Compagnie Aérienne", true)]
        [InlineData("Terrestre", false)]
        [InlineData(null, false)]
        public void IsAerialVehicle_detects_libelle(string? libelle, bool expected)
        {
            var billet = BuildBillet(typeLibelle: libelle);
            Assert.Equal(expected, BilletReportService.IsAerialVehicle(billet));
        }

        private static Billet BuildBillet(string? typeLibelle)
        {
            return new Billet
            {
                IdBillet = 42,
                IdReservation = 100,
                IdReservationPassenger = 7,
                IdSite = 3,
                QrCode = "QR-UNIT-TEST-42",
                CodeSiege = "12A",
                IdSociete = 1,
                Societe = new Societe { IdSociete = 1, Nom = "Air Congo Test" },
                Site = new Site { IdSite = 3, NomSite = "Aéroport Maya-Maya" },
                ReservationPassenger = new ReservationPassenger
                {
                    IdReservationPassenger = 7,
                    NomComplet = "Passager Test",
                    Email = "passager@test.cg",
                    Telephone = "+242060000001"
                },
                Siege = new Siege
                {
                    IdSiege = 9,
                    CodeSiege = "12A",
                    IdCategorieSiege = 1,
                    CategorieSiege = new CategorieSiege { IdCategorieSiege = 1, Libelle = "Economy", CodeCategorieSiege = "ECO" }
                },
                Reservation = new Reservation
                {
                    IdReservation = 100,
                    Client = new Client { NomClient = "Client Test", Telephone = "+242060000000" },
                    Voyage = new Voyage
                    {
                        DateDepart = new DateTime(2026, 8, 15),
                        HeureDepart = new TimeSpan(8, 30, 0),
                        Prix = 15000,
                        Destination = new Destination { VilleDepart = "Brazzaville", VilleArrivee = "Pointe-Noire" },
                        Vehicule = new Vehicule
                        {
                            AliasVehicule = "CG-101",
                            TypeVehicule = typeLibelle == null
                                ? null
                                : new TypeVehicule { Libelle = typeLibelle }
                        }
                    }
                }
            };
        }

        private static BilletReportService CreateService(IBilletRepository repo)
        {
            var configRepo = new Mock<IConfigSocieteRepository>();
            var env = new Mock<IWebHostEnvironment>();
            env.SetupGet(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());
            return new BilletReportService(
                repo,
                configRepo.Object,
                env.Object,
                NullLogger<BilletReportService>.Instance);
        }
    }
}
