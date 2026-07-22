namespace CongoTravel.Models.DTOs
{
    public class TypeVehiculeResponseDto
    {
        public int IdTypeVehicule { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public int IdSociete { get; set; }
        public bool Statut { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
    }
}
