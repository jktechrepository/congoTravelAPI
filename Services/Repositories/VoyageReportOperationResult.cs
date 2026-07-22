using CongoTravel.Models.DTOs.Voyage;

namespace CongoTravel.Services.Repositories
{
    public sealed class VoyageReportOperationResult
    {
        public bool Success { get; init; }
        public int StatusCode { get; init; } = 200;
        public string Message { get; init; } = string.Empty;
        public ReporterVoyageResultDto? Data { get; init; }
        public IReadOnlyList<int>? BilletsUtilises { get; init; }

        public static VoyageReportOperationResult Ok(ReporterVoyageResultDto data) => new()
        {
            Success = true,
            StatusCode = 200,
            Message = "Voyage reporté avec succès.",
            Data = data
        };

        public static VoyageReportOperationResult Fail(int statusCode, string message, IReadOnlyList<int>? billetsUtilises = null) => new()
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            BilletsUtilises = billetsUtilises
        };
    }
}
