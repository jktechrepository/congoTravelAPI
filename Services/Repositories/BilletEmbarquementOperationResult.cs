using CongoTravel.Models;

namespace CongoTravel.Services.Repositories
{
    /// <summary>Résultat de <see cref="IBilletRepository.EnregistrerEmbarquementAsync"/>.</summary>
    public sealed class BilletEmbarquementOperationResult
    {
        public bool Success { get; init; }
        public int StatusCode { get; init; }
        public string Message { get; init; } = "";
        public Billet? Billet { get; init; }
        public BilletEmbarquement? Embarquement { get; init; }
    }
}
