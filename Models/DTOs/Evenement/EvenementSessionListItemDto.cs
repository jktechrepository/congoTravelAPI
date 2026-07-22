namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>En-tête session événement pour les listes (sans inventaire imbriqué).</summary>
    public class EvenementSessionListItemDto
    {
        public int IdEvenementSession { get; set; }

        public int IdSociete { get; set; }

        public string CodeSession { get; set; } = string.Empty;

        public string Libelle { get; set; } = string.Empty;

        public DateTime StartAtUtc { get; set; }

        public DateTime? EndAtUtc { get; set; }

        public string InventoryMode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime DateCreation { get; set; }

        public DateTime? DateModification { get; set; }
    }
}
