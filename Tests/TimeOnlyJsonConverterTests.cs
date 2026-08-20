using System.Text.Json;
using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant.Enums;
using Xunit;

namespace CongoTravel.Tests
{
    public class TimeOnlyJsonConverterTests
    {
        private static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            options.Converters.Add(new TimeOnlyJsonConverter());
            options.Converters.Add(new NullableTimeOnlyJsonConverter());
            return options;
        }

        [Theory]
        [InlineData("\"12:00:00\"", 12, 0, 0)]
        [InlineData("\"12:00\"", 12, 0, 0)]
        [InlineData("\"14:30:00\"", 14, 30, 0)]
        [InlineData("\"19:05:45\"", 19, 5, 45)]
        [InlineData("\"12:00:00.0000000\"", 12, 0, 0)]
        public void Deserialize_TimeOnly_Accepts_CommonFormats(string json, int h, int m, int s)
        {
            var value = JsonSerializer.Deserialize<TimeOnly>(json, CreateOptions());
            Assert.Equal(new TimeOnly(h, m, s), value);
        }

        [Fact]
        public void Deserialize_TimeOnly_Accepts_IsoDateTimePrefix()
        {
            var value = JsonSerializer.Deserialize<TimeOnly>("\"2026-09-15T12:30:00\"", CreateOptions());
            Assert.Equal(new TimeOnly(12, 30, 0), value);
        }

        [Fact]
        public void Serialize_TimeOnly_Writes_HHmmss()
        {
            var json = JsonSerializer.Serialize(new TimeOnly(9, 5, 7), CreateOptions());
            Assert.Equal("\"09:05:07\"", json);
        }

        [Fact]
        public void Deserialize_NullableTimeOnly_Accepts_Null()
        {
            var value = JsonSerializer.Deserialize<TimeOnly?>("null", CreateOptions());
            Assert.Null(value);
        }

        [Fact]
        public void Deserialize_RestaurantPlanification_Accepts_Hm_And_Hms()
        {
            const string json =
                "{\"libelle\":\"Service week-end\",\"idRestaurant\":1,\"joursSemaine\":[5,6],\"inventoryMode\":\"GlobalQuota\",\"codeDevise\":\"CDF\",\"montantAcompte\":null,\"statut\":true,\"plages\":[{\"ordre\":0,\"libelle\":\"Midi\",\"startTime\":\"12:00\",\"endTime\":\"14:30:00\",\"globalQuota\":{\"capaciteTotale\":40,\"prixUnitaire\":25000}},{\"ordre\":1,\"libelle\":\"Soir\",\"startTime\":\"19:00:00\",\"endTime\":\"22:00\",\"globalQuota\":{\"capaciteTotale\":50,\"prixUnitaire\":30000}}]}";

            var dto = JsonSerializer.Deserialize<RestaurantCreatePlanificationRequestDto>(json, CreateOptions());

            Assert.NotNull(dto);
            Assert.Equal("Service week-end", dto!.Libelle);
            Assert.Equal(RestaurantInventoryMode.GlobalQuota, dto.InventoryMode);
            Assert.Equal(2, dto.Plages.Count);
            Assert.Equal(new TimeOnly(12, 0), dto.Plages[0].StartTime);
            Assert.Equal(new TimeOnly(14, 30), dto.Plages[0].EndTime);
            Assert.Equal(new TimeOnly(19, 0), dto.Plages[1].StartTime);
            Assert.Equal(new TimeOnly(22, 0), dto.Plages[1].EndTime);
            Assert.NotNull(dto.Plages[0].GlobalQuota);
            Assert.Equal(40, dto.Plages[0].GlobalQuota!.CapaciteTotale);
        }

        [Fact]
        public void RoundTrip_RestaurantPlanification_StartEndTimes()
        {
            var options = CreateOptions();
            var original = new RestaurantCreatePlanificationRequestDto
            {
                Libelle = "Roundtrip",
                IdRestaurant = 2,
                JoursSemaine = new List<int> { 1 },
                InventoryMode = RestaurantInventoryMode.GlobalQuota,
                CodeDevise = "USD",
                Plages = new List<RestaurantCreatePlanificationPlageDto>
                {
                    new()
                    {
                        Ordre = 0,
                        StartTime = new TimeOnly(11, 15, 30),
                        EndTime = new TimeOnly(13, 45, 0),
                        GlobalQuota = new RestaurantCreatePlanificationGlobalQuotaDto
                        {
                            CapaciteTotale = 10,
                            PrixUnitaire = 5
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(original, options);
            var restored = JsonSerializer.Deserialize<RestaurantCreatePlanificationRequestDto>(json, options);

            Assert.NotNull(restored);
            Assert.Equal(original.Plages[0].StartTime, restored!.Plages[0].StartTime);
            Assert.Equal(original.Plages[0].EndTime, restored.Plages[0].EndTime);
            Assert.Contains("\"11:15:30\"", json);
            Assert.Contains("\"13:45:00\"", json);
        }
    }
}
