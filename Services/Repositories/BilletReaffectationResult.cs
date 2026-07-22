using CongoTravel.Models;

namespace CongoTravel.Services.Repositories
{
    public sealed class BilletReaffectationResult
    {
        public bool Success { get; init; }
        public int StatusCode { get; init; }
        public string Message { get; init; } = "";
        public decimal DifferentielTarifaire { get; init; }
        public decimal Penalite { get; init; }
        public bool PenaliteAppliquee { get; init; }
        public decimal? PenalitePourcentageApplique { get; init; }
        public decimal MontantPayeReference { get; init; }
        public decimal MontantTotalRegularisation { get; init; }
        public bool PaiementDifferentielRequis { get; init; }
        public bool PaiementDifferentielConfirme { get; init; }
        public int? IdAncienVoyage { get; init; }
        public int? IdNouveauVoyage { get; init; }
        public int? HeuresLimiteReaffectation { get; init; }
        public DateTime? DepartVoyageSource { get; init; }
        public DateTime? DeadlineReaffectation { get; init; }
        public Billet? Billet { get; init; }
    }
}
