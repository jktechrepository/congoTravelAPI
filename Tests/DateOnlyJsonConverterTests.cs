using System.Text.Json;
using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.DTOs.SiteTouristique;
using Xunit;

namespace CongoTravel.Tests
{
    public class DateOnlyJsonConverterTests
    {
        private static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new DateOnlyJsonConverter());
            options.Converters.Add(new NullableDateOnlyJsonConverter());
            return options;
        }

        [Fact]
        public void Deserialize_SiteTouristiqueJournee_Accepts_YyyyMmDd()
        {
            const string json =
                "{\"idSiteTouristique\":1,\"dateVisite\":\"2026-09-15\",\"inventoryMode\":\"GlobalQuota\",\"codeDevise\":\"CDF\",\"globalQuota\":{\"capaciteTotale\":100,\"prixUnitaire\":500}}";

            var dto = JsonSerializer.Deserialize<SiteTouristiqueCreateJourneeRequestDto>(json, CreateOptions());

            Assert.NotNull(dto);
            Assert.Equal(1, dto!.IdSiteTouristique);
            Assert.Equal(new DateOnly(2026, 9, 15), dto.DateVisite);
            Assert.Equal("GlobalQuota", dto.InventoryMode);
            Assert.NotNull(dto.GlobalQuota);
            Assert.Equal(100, dto.GlobalQuota!.CapaciteTotale);
            Assert.Equal(500m, dto.GlobalQuota.PrixUnitaire);
        }

        [Fact]
        public void Deserialize_SiteTouristiqueJournee_Accepts_IsoDateTime()
        {
            const string json =
                "{\"idSiteTouristique\":1,\"dateVisite\":\"2026-09-15T00:00:00Z\",\"inventoryMode\":\"GlobalQuota\",\"codeDevise\":\"CDF\",\"globalQuota\":{\"capaciteTotale\":10,\"prixUnitaire\":1}}";

            var dto = JsonSerializer.Deserialize<SiteTouristiqueCreateJourneeRequestDto>(json, CreateOptions());

            Assert.NotNull(dto);
            Assert.Equal(new DateOnly(2026, 9, 15), dto!.DateVisite);
        }

        [Fact]
        public void Deserialize_RestaurantCreneau_Accepts_DateService_YyyyMmDd()
        {
            const string json =
                "{\"idRestaurant\":1,\"dateService\":\"2026-09-15\",\"startAtUtc\":\"2026-09-15T18:00:00Z\",\"endAtUtc\":\"2026-09-15T21:00:00Z\",\"inventoryMode\":\"GlobalQuota\",\"codeDevise\":\"CDF\",\"globalQuota\":{\"capaciteTotale\":20,\"prixUnitaire\":25000}}";

            var dto = JsonSerializer.Deserialize<RestaurantCreateCreneauRequestDto>(json, CreateOptions());

            Assert.NotNull(dto);
            Assert.Equal(new DateOnly(2026, 9, 15), dto!.DateService);
        }

        [Fact]
        public void Serialize_DateOnly_Writes_YyyyMmDd()
        {
            var options = CreateOptions();
            var json = JsonSerializer.Serialize(new DateOnly(2026, 9, 15), options);
            Assert.Equal("\"2026-09-15\"", json);
        }
    }
}
