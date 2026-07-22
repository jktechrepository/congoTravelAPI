using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using Xunit;

namespace CongoTravel.Tests
{
    public class BilletPassengerIdentityCompatTests
    {
        [Fact]
        public void ApplyPassengerIdentityToClientFields_overwrites_with_passenger()
        {
            var dto = new BilletResponseDto
            {
                NomClient = "Acheteur Dupont",
                TelephoneClient = "+243111"
            };
            var billet = new Billet
            {
                ReservationPassenger = new ReservationPassenger
                {
                    NomComplet = "Passager Réel",
                    Telephone = "+243999"
                }
            };

            BilletPassengerIdentityCompat.ApplyPassengerIdentityToClientFields(dto, billet);

            Assert.Equal("Passager Réel", dto.NomClient);
            Assert.Equal("+243999", dto.TelephoneClient);
        }

        [Fact]
        public void ApplyPassengerIdentityToClientFields_leaves_dto_unchanged_when_no_passenger()
        {
            var dto = new BilletResponseDto
            {
                NomClient = "Acheteur Dupont",
                TelephoneClient = "+243111"
            };
            var billet = new Billet();

            BilletPassengerIdentityCompat.ApplyPassengerIdentityToClientFields(dto, billet);

            Assert.Equal("Acheteur Dupont", dto.NomClient);
            Assert.Equal("+243111", dto.TelephoneClient);
        }
    }
}
