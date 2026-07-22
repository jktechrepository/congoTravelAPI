using CongoTravel.Models;
using CongoTravel.Models.DTOs;

namespace CongoTravel.Helpers
{
    /// <summary>
    /// Aligne <see cref="BilletResponseDto.NomClient"/> / <see cref="BilletResponseDto.TelephoneClient"/>
    /// sur le passager transporté (compatibilité frontend embarquement).
    /// </summary>
    public static class BilletPassengerIdentityCompat
    {
        public static void ApplyPassengerIdentityToClientFields(BilletResponseDto dto, Billet billet)
        {
            if (billet.ReservationPassenger == null)
                return;

            dto.NomClient = billet.ReservationPassenger.NomComplet;
            dto.TelephoneClient = billet.ReservationPassenger.Telephone;
        }
    }
}
