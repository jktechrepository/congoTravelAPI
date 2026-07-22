namespace CongoTravel.Models.DTOs
{
    public class ReaffecterBilletRequestDto
    {
        public int IdVoyageCible { get; set; }
        public bool ConfirmerPaiementDifferentiel { get; set; }
        public string? MethodePaiement { get; set; }
        public string? ReferenceTransaction { get; set; }
        public string? Commentaire { get; set; }
    }
}
