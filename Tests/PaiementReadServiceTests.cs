using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class PaiementReadServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task GetByIdAsync_includes_nom_client_for_response_mapping()
        {
            await using var ctx = BuildDb(nameof(GetByIdAsync_includes_nom_client_for_response_mapping));

            ctx.Societes.Add(new Societe { IdSociete = 1, Nom = "Soc", Statut = true, DateCreation = DateTime.UtcNow });
            ctx.Clients.Add(new Client
            {
                IdClient = 3,
                NomClient = "Jean Dupont",
                AdresseClient = "Adresse",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            ctx.Utilisateurs.Add(new Utilisateur
            {
                IdUtilisateur = 6,
                NomComplet = "Caissier",
                Email = "c@test.com",
                MotDePasseHash = "hash",
                DateCreation = DateTime.UtcNow
            });
            ctx.Reservations.Add(new Reservation
            {
                IdReservation = 167,
                IdClient = 3,
                IdUtilisateur = 6,
                IdVoyage = 1,
                IdSociete = 1,
                Statut = true,
                StatutReservation = "CONFIRMEE",
                DateReservation = DateTime.UtcNow,
                NombreDePlace = 1
            });
            ctx.Paiements.Add(new Paiement
            {
                IdPaiement = 127,
                IdReservation = 167,
                IdUtilisateur = 6,
                IdSociete = 1,
                MontantAPaye = 700,
                MontantPaye = 700,
                CodeDevisePaiement = "CDF",
                CodeDevisePrincipale = "CDF",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var svc = new PaiementService(
                ctx,
                NullLogger<PaiementService>.Instance,
                new BilletEmissionService(
                    Mock.Of<IBilletRepository>(),
                    Mock.Of<IQrCodeService>(),
                    ctx,
                    Mock.Of<IConfigSocieteRepository>(),
                    NullLogger<BilletEmissionService>.Instance));

            var paiement = await svc.GetByIdAsync(127);
            var dto = PaiementApiResponseMapper.Map(paiement!);

            Assert.Equal("Jean Dupont", dto.NomClient);
            Assert.Equal(3, dto.IdClient);
        }
    }
}
