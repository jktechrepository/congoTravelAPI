namespace CongoTravel.Models.DTOs
{
    public class BilletResponseDto
    {
        public int IdBillet { get; set; }
        public bool IsUsed { get; set; }
        public int? IdReservation { get; set; }
        public int? IdReservationPassenger { get; set; }
        public int? IdSiege { get; set; }
        public string? CodeSiege { get; set; }
        public string? NomPassager { get; set; }
        public string QrCode { get; set; }
        public DateTime DateGeneration { get; set; }
        public DateTime? DateValiditeDebut { get; set; }
        public DateTime? DateValiditeFin { get; set; }
        public int IdSociete { get; set; }

        /// <summary>Site (optionnel).</summary>
        public int? IdSite { get; set; }

        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }

        // Navigation properties optionnelles
        public string? StatutReservation { get; set; }
        public DateTime? DateReservation { get; set; }
        public string? NomUtilisateur { get; set; }
        public string? EmailUtilisateur { get; set; }
        public string? NomClient { get; set; }
        public string? TelephoneClient { get; set; }
        public DateTime? DateVoyage { get; set; }
        public TimeSpan? HeureVoyage { get; set; }
        /// <summary>Prix unitaire du billet (tarif de la catégorie du siège attribué à ce passager), pas le total de la réservation.</summary>
        public int? PrixVoyage { get; set; }
        public decimal KiloBagageOffert { get; set; }
        public string? LogoSociete { get; set; }
        public string? AliasVehicule { get; set; }
        public string? VilleDepart { get; set; }
        public string? VilleArrivee { get; set; }
    }
}
