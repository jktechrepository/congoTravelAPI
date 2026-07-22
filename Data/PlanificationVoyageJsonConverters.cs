using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CongoTravel.Data
{
    internal static class PlanificationVoyageJsonConverters
    {
        private static readonly JsonSerializerOptions Options = new();

        public static readonly ValueComparer<List<int>> JoursSemaineComparer = new(
            (a, b) => (a ?? new List<int>()).SequenceEqual(b ?? new List<int>()),
            v => (v ?? new List<int>()).Aggregate(0, (h, i) => HashCode.Combine(h, i)),
            v => (v ?? new List<int>()).ToList());

        public static string SerializeJoursSemaine(List<int> value) =>
            JsonSerializer.Serialize(value, Options);

        public static List<int> DeserializeJoursSemaine(string value) =>
            JsonSerializer.Deserialize<List<int>>(value, Options) ?? new List<int>();
    }
}
