namespace CongoTravel.Models.DTOs
{
    /// <summary>Passager ayant enregistré un embarquement (scan billet) pour un voyage donné.</summary>
    public class PassagerEmbarqueVoyageItemDto
    {
        public int IdEmbarquement { get; set; }

        public DateTime DateEmbarquementUtc { get; set; }

        public int IdBillet { get; set; }

        public int IdReservationPassenger { get; set; }

        public int IdReservation { get; set; }

        /// <summary>Identifiant du voyage lié à la réservation.</summary>
        public int IdVoyage { get; set; }

        public string NomComplet { get; set; } = string.Empty;

        public string? Telephone { get; set; }

        public int? IdUtilisateurEnregistrement { get; set; }
    }
}
