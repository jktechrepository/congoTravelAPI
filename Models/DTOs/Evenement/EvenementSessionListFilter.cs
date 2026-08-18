using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Filtres optionnels pour la liste des sessions événement.</summary>
    public class EvenementSessionListFilter
    {
        public EvenementSessionStatus? Status { get; set; }

        public EvenementInventoryMode? InventoryMode { get; set; }

        public EvenementSessionType? TypeEvenement { get; set; }

        /// <summary>Filtre optionnel catalogue public (Clients / anonymes).</summary>
        public int? IdSociete { get; set; }
    }
}
