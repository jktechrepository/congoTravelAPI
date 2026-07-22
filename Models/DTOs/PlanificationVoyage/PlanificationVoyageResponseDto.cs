using CongoTravel.Models.DTOs.VoyageTarification;

namespace CongoTravel.Models.DTOs.PlanificationVoyage
{
    public class PlanificationVoyageResponseDto
    {
        public int IdPlanificationVoyage { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public int IdSociete { get; set; }
        public int IdSite { get; set; }
        public int IdVehicule { get; set; }
        public TimeSpan HeureDepart { get; set; }
        public int Prix { get; set; }
        public string CodeDevisePrix { get; set; } = "CDF";
        public List<int> JoursSemaine { get; set; } = new();
        public bool Statut { get; set; }
        public int? IdDestination { get; set; }
        public List<PlanificationVoyageEtapeDto> EtapesDestinations { get; set; } = new();
        public List<VoyageTarifCategorieSiegeItemDto> Tarifs { get; set; } = new();
        public int NombreVoyagesGeneres { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
    }
}
