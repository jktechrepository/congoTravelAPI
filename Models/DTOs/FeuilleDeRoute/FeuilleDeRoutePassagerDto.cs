namespace CongoTravel.Models.DTOs.FeuilleDeRoute
{
    public class FeuilleDeRoutePassagerDto
    {
        public int IdFeuilleDeRoutePassager { get; set; }

        public int? IdEmbarquement { get; set; }

        public int? IdBillet { get; set; }

        public int? IdReservationPassenger { get; set; }

        public int? IdReservation { get; set; }

        public string NomComplet { get; set; } = string.Empty;

        public string? Telephone { get; set; }

        public string? Email { get; set; }

        public string? DocumentType { get; set; }

        public string? DocumentNumero { get; set; }

        public string? CodeSiege { get; set; }

        public DateTime? DateEmbarquementUtc { get; set; }

        public int? IdUtilisateurEnregistrement { get; set; }
    }
}
