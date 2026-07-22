using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.VoyageTarification
{
    public class VoyageTarifCategorieSiegeReadDto
    {
        public int IdVoyageTarifCategorieSiege { get; set; }
        public int IdCategorieSiege { get; set; }
        public string CodeCategorieSiege { get; set; } = string.Empty;
        public string LibelleCategorie { get; set; } = string.Empty;
        public int Prix { get; set; }
    }

    public class VoyageTarifCategorieSiegeItemDto
    {
        [Range(1, int.MaxValue)]
        public int IdCategorieSiege { get; set; }

        [Range(0, int.MaxValue)]
        public int Prix { get; set; }
    }

    public class VoyageTarifCategorieSiegeResponseItemDto
    {
        public int IdCategorieSiege { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public int Prix { get; set; }
    }

    public class VoyageTarifsCategorieSiegeUpsertDto
    {
        [Required]
        [MinLength(1)]
        public List<VoyageTarifCategorieSiegeItemDto> Tarifs { get; set; } = new();
    }

    /// <summary>Mise à jour du tarif pour une seule catégorie de siège (PATCH).</summary>
    public class VoyageTarifCategorieSiegePatchDto
    {
        [Range(0, int.MaxValue)]
        public int Prix { get; set; }
    }
}
