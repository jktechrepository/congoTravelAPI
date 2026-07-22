namespace CongoTravel.Models.DTOs.Evenement
{
    public class EvenementClasseResponseDto
    {
        public int IdEvenementClasse { get; set; }

        public int IdSociete { get; set; }

        public string CodeClasse { get; set; } = string.Empty;

        public string Libelle { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool Statut { get; set; }

        public DateTime DateCreation { get; set; }
    }
}
