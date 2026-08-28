using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;

namespace CongoTravel.Helpers.Hotel
{
    public static class HotelReservationMapper
    {
        public static HotelReservationResponseDto ToResponse(HotelReservation r) => new()
        {
            IdHotelReservation = r.IdHotelReservation,
            IdSociete = r.IdSociete,
            IdHotel = r.IdHotel,
            IdSite = r.IdSite,
            IdUtilisateur = r.IdUtilisateur,
            IdClient = r.IdClient,
            ReferenceReservation = r.ReferenceReservation,
            CustomerRef = r.CustomerRef,
            CheckInDate = r.CheckInDate,
            CheckOutDate = r.CheckOutDate,
            NombreNuits = r.NombreNuits,
            Status = r.Status.ToString(),
            ExpiresAtUtc = r.ExpiresAtUtc,
            CheckedInAtUtc = r.CheckedInAtUtc,
            CheckedOutAtUtc = r.CheckedOutAtUtc,
            MontantSejour = r.MontantSejour,
            MontantSousTotal = r.MontantSousTotal,
            CodeDevise = r.CodeDevise,
            InventoryMode = r.InventoryMode.ToString(),
            DateCreation = r.DateCreation,
            DateModification = r.DateModification,
            Lines = r.Lines.Select(l => new HotelReservationLineResponseDto
            {
                IdHotelReservationLine = l.IdHotelReservationLine,
                LineType = l.LineType.ToString(),
                IdHotelRoomType = l.IdHotelRoomType,
                IdHotelNight = l.IdHotelNight,
                Quantity = l.Quantity,
                PrixSejourUnitaire = l.PrixSejourUnitaire,
                MontantLigne = l.MontantLigne,
                CodeDevise = l.CodeDevise
            }).ToList(),
            Payments = r.Payments.Select(ToPayment).ToList(),
            RoomAssignments = (r.RoomAssignments ?? Enumerable.Empty<HotelRoomAssignment>())
                .Select(HotelRoomMapper.ToAssignmentDto).ToList(),
            Extras = (r.ReservationExtras ?? Enumerable.Empty<HotelReservationExtra>())
                .Select(HotelExtraMapper.ToReservationExtraDto).ToList(),
            MontantExtras = (r.ReservationExtras ?? Enumerable.Empty<HotelReservationExtra>())
                .Sum(e => e.MontantLigne)
        };

        public static HotelPaymentResponseDto ToPayment(HotelPayment p) => new()
        {
            IdHotelPayment = p.IdHotelPayment,
            ReferencePaiement = p.ReferencePaiement,
            Provider = p.Provider,
            ProviderTxRef = p.ProviderTxRef,
            Status = p.Status.ToString(),
            Montant = p.Montant,
            CodeDevise = p.CodeDevise,
            DateCreation = p.DateCreation
        };

        public static HotelHoldResponseDto ToHold(HotelReservation r) => new()
        {
            IdHotelReservation = r.IdHotelReservation,
            ReferenceReservation = r.ReferenceReservation,
            Status = r.Status.ToString(),
            ExpiresAtUtc = r.ExpiresAtUtc,
            MontantSejour = r.MontantSejour,
            MontantSousTotal = r.MontantSousTotal,
            CodeDevise = r.CodeDevise
        };
    }
}
