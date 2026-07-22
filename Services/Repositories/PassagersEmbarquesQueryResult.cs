using CongoTravel.Models.DTOs;

namespace CongoTravel.Services.Repositories
{
    /// <summary>Résultat de <see cref="IVoyageRepository.GetPassagersEmbarquesPourCriteresVoyageAsync"/>.</summary>
    public sealed class PassagersEmbarquesQueryResult
    {
        public bool Success { get; init; }
        public int ErrorStatusCode { get; init; }
        public string ErrorMessage { get; init; } = "";

        public IReadOnlyList<PassagerEmbarqueVoyageItemDto> Items { get; init; } =
            Array.Empty<PassagerEmbarqueVoyageItemDto>();

        public static PassagersEmbarquesQueryResult Ok(IReadOnlyList<PassagerEmbarqueVoyageItemDto> items) =>
            new() { Success = true, Items = items };

        public static PassagersEmbarquesQueryResult NoVoyage(string message) =>
            new() { Success = false, ErrorStatusCode = 404, ErrorMessage = message };

        public static PassagersEmbarquesQueryResult AmbiguousVoyages(string message) =>
            new() { Success = false, ErrorStatusCode = 400, ErrorMessage = message };
    }
}
