namespace CongoTravel.Services.Repositories
{
    public interface IBilletReportService
    {
        /// <summary>
        /// Génère le PDF A4 aérien (<c>Reports/Billet_A4.frx</c>).
        /// Réservé aux billets dont le véhicule a un type aérien.
        /// </summary>
        Task<BilletPdfGenerationOutcome> GeneratePdfAsync(int idBillet, CancellationToken cancellationToken = default);

        /// <summary>
        /// Génère une prévisualisation HTML du billet A4 aérien (affichage navigateur).
        /// </summary>
        Task<BilletPdfGenerationOutcome> GenerateHtmlPreviewAsync(int idBillet, CancellationToken cancellationToken = default);
    }

    public enum BilletPdfOutcomeStatus
    {
        Success,
        NotFound,
        NotAerial
    }

    public sealed class BilletPdfGenerationOutcome
    {
        public BilletPdfOutcomeStatus Status { get; init; }
        public BilletPdfResult? Pdf { get; init; }
        public string? Message { get; init; }

        public static BilletPdfGenerationOutcome Success(BilletPdfResult pdf) => new()
        {
            Status = BilletPdfOutcomeStatus.Success,
            Pdf = pdf
        };

        public static BilletPdfGenerationOutcome NotFound(int idBillet) => new()
        {
            Status = BilletPdfOutcomeStatus.NotFound,
            Message = $"Billet avec l'ID {idBillet} non trouvé"
        };

        public static BilletPdfGenerationOutcome NotAerial(string message) => new()
        {
            Status = BilletPdfOutcomeStatus.NotAerial,
            Message = message
        };
    }

    public sealed class BilletPdfResult
    {
        public byte[] Content { get; init; } = Array.Empty<byte>();
        public string FileName { get; init; } = "billet_d_avion_a4.pdf";
        public string ContentType { get; init; } = "application/pdf";
    }
}
