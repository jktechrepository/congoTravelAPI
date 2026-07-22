namespace CongoTravel.Models.DTOs
{
    /// <summary>Réponse après enregistrement d’un embarquement (scan billet).</summary>
    public class EmbarquerBilletResponseDto
    {
        public int IdEmbarquement { get; set; }
        public DateTime DateEmbarquementUtc { get; set; }
        public int? IdUtilisateurEnregistrement { get; set; }
        public BilletResponseDto Billet { get; set; } = null!;
    }
}
