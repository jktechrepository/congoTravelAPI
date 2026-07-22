namespace CongoTravel.Models.DTOs.Reservation
{
    /// <summary>
    /// Passager lié à une réservation (lecture API).
    /// </summary>
    public class ReservationPassengerReadDto
    {
        public int IdReservationPassenger { get; set; }
        public int IdReservation { get; set; }
        public int? IdClient { get; set; }
        public string NomComplet { get; set; } = string.Empty;
        public string? Telephone { get; set; }
        public string? Email { get; set; }
        public string? DocumentType { get; set; }
        public string? DocumentNumero { get; set; }
        public DateTime? DateNaissance { get; set; }
        public string? Genre { get; set; }
        public int IdSociete { get; set; }
        public bool Statut { get; set; }
    }
}
