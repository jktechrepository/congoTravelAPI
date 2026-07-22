using CongoTravel.Helpers;
using CongoTravel.Models;
using Xunit;

namespace CongoTravel.Tests
{
    public class PaiementApiResponseMapperTests
    {
        [Fact]
        public void Map_includes_nom_client_from_reservation()
        {
            var dto = PaiementApiResponseMapper.Map(new Paiement
            {
                IdPaiement = 127,
                IdUtilisateur = 6,
                IdSociete = 1,
                IdReservation = 167,
                MontantAPaye = 700,
                MontantPaye = 700,
                Statut = true,
                Origine = Models.Enums.OrigineOperation.CAISSIER,
                DateCreation = DateTime.UtcNow,
                Utilisateur = new Utilisateur { NomComplet = "Caissier Test" },
                Societe = new Societe { Nom = "Rusa Demo" },
                Reservation = new Reservation
                {
                    IdReservation = 167,
                    IdClient = 3,
                    Client = new Client { NomClient = "Jean Dupont" }
                }
            });

            Assert.Equal(3, dto.IdClient);
            Assert.Equal("Jean Dupont", dto.NomClient);
            Assert.Equal("Caissier Test", dto.NomUtilisateur);
            Assert.Equal("RES-000167", dto.CodeReservation);
            Assert.Equal(Models.Enums.OrigineOperation.CAISSIER, dto.Origine);
            Assert.Equal(Models.Enums.OrigineOperationGroupe.AGENT, dto.OrigineGroupe);
        }

        [Fact]
        public void Map_nom_client_null_when_no_reservation()
        {
            var dto = PaiementApiResponseMapper.Map(new Paiement
            {
                IdPaiement = 1,
                IdUtilisateur = 1,
                IdSociete = 1,
                MontantAPaye = 100,
                Statut = true,
                DateCreation = DateTime.UtcNow
            });

            Assert.Null(dto.IdClient);
            Assert.Null(dto.NomClient);
        }
    }
}
