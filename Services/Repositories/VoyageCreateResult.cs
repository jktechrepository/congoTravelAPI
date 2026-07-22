using CongoTravel.Models;
using CongoTravel.Models.DTOs;

namespace CongoTravel.Services.Repositories
{
    public enum VoyageCreateOutcome
    {
        Created,
        SkippedConflict,
        Failed
    }

    public class VoyageCreateOptions
    {
        public int? IdPlanificationVoyage { get; set; }
        public bool ThrowOnConflict { get; set; } = true;
    }

    public class VoyageCreateResult
    {
        public VoyageCreateOutcome Outcome { get; set; }
        public Voyage? Voyage { get; set; }
        public string? Message { get; set; }

        public static VoyageCreateResult Created(Voyage voyage) =>
            new() { Outcome = VoyageCreateOutcome.Created, Voyage = voyage };

        public static VoyageCreateResult Skipped(string message) =>
            new() { Outcome = VoyageCreateOutcome.SkippedConflict, Message = message };

        public static VoyageCreateResult Failed(string message) =>
            new() { Outcome = VoyageCreateOutcome.Failed, Message = message };
    }
}
