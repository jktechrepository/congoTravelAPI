using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Reservation;
using CongoTravel.Models.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class OrigineOperationTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Theory]
        [InlineData(UserRoles.CLIENT, OrigineOperation.CLIENT)]
        [InlineData(UserRoles.CAISSIER, OrigineOperation.CAISSIER)]
        [InlineData(UserRoles.GERANT, OrigineOperation.GERANT)]
        [InlineData(UserRoles.SOUS_DIRECTEUR, OrigineOperation.GERANT)]
        [InlineData(UserRoles.ADMIN, OrigineOperation.ADMIN)]
        [InlineData(UserRoles.FINANCIER, OrigineOperation.FINANCIER)]
        [InlineData(UserRoles.SECRETAIRE, OrigineOperation.SECRETAIRE)]
        [InlineData(UserRoles.SUPER_ADMIN, OrigineOperation.SUPER_ADMIN)]
        public void ResolveFromRole_maps_known_roles(string role, string expected)
        {
            Assert.Equal(expected, OrigineOperationResolver.ResolveFromRole(role, isStaff: true));
        }

        [Fact]
        public void Resolve_unauthenticated_returns_INCONNU()
        {
            Assert.Equal(OrigineOperation.INCONNU, OrigineOperationResolver.Resolve(CurrentUserTestHelper.MockUnauthenticated()));
        }

        [Fact]
        public void ResolveForPaiement_inherits_reservation_origine()
        {
            var currentUser = CurrentUserTestHelper.MockCaissier();
            var origine = OrigineOperationResolver.ResolveForPaiement(currentUser, OrigineOperation.CLIENT);
            Assert.Equal(OrigineOperation.CLIENT, origine);
        }

        [Fact]
        public async Task PaiementService_CreateAsync_inherits_origine_from_existing_reservation()
        {
            await using var ctx = BuildDb(nameof(PaiementService_CreateAsync_inherits_origine_from_existing_reservation));

            ctx.Societes.Add(new Societe { IdSociete = 1, Nom = "Soc", Statut = true, DateCreation = DateTime.UtcNow });
            ctx.Reservations.Add(new Reservation
            {
                IdReservation = 50,
                IdClient = 1,
                IdUtilisateur = 1,
                IdVoyage = 1,
                IdSociete = 1,
                Statut = true,
                StatutReservation = "CONFIRMEE",
                DateReservation = DateTime.UtcNow,
                NombreDePlace = 1,
                Origine = OrigineOperation.CLIENT
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

            var paiement = await svc.CreateAsync(new Paiement
            {
                IdReservation = 50,
                IdUtilisateur = 2,
                IdSociete = 1,
                MontantAPaye = 1000,
                MontantPaye = 500,
                CodeDevisePaiement = "CDF",
                CodeDevisePrincipale = "CDF",
                MontantAPayeDevisePrincipale = 1000,
                Statut = true
            });

            Assert.Equal(OrigineOperation.CLIENT, paiement.Origine);
        }

    }
}
