using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Mapping;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using System.Text.Json;
using Xunit;

namespace CongoTravel.Tests
{
    public class VehiculePhotoPayloadTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        [Fact]
        public void CreateVehiculeDto_legacy_photo_maps_to_persistence()
        {
            const string json =
                "{\"aliasVehicule\":\"B1\",\"idTypeVehicule\":1,\"nombreSiege\":10,\"idSociete\":1," +
                "\"numeroDePlaque\":\"X-1\",\"photo\":\"data:image/jpeg;base64,/9j/4AAQ\"}";

            var dto = JsonSerializer.Deserialize<CreateVehiculeDto>(json, JsonOptions)!;
            var photos = dto.ResolvePhotosForPersistence();

            Assert.NotNull(photos);
            Assert.Single(photos);
            Assert.StartsWith("data:image/jpeg;base64,", photos[0].PhotoBase64);
        }

        [Fact]
        public void CreateVehiculeDto_photos_array_of_strings_deserializes()
        {
            const string json =
                "{\"aliasVehicule\":\"B1\",\"idTypeVehicule\":1,\"nombreSiege\":10,\"idSociete\":1," +
                "\"numeroDePlaque\":\"X-1\",\"photos\":[\"aGVsbG8=\",\"data:image/png;base64,AAAA\"]}";

            var dto = JsonSerializer.Deserialize<CreateVehiculeDto>(json, JsonOptions)!;
            var photos = dto.ResolvePhotosForPersistence();

            Assert.NotNull(photos);
            Assert.Equal(2, photos.Count);
            Assert.Equal("aGVsbG8=", photos[0].PhotoBase64);
        }

        [Fact]
        public void AddPhotoVehiculeDto_filePath_alias_binds_to_photoBase64()
        {
            const string json = "{ \"filePath\": \"data:image/jpeg;base64,abc\" }";

            var dto = JsonSerializer.Deserialize<AddPhotoVehiculeDto>(json, JsonOptions)!;

            Assert.Equal("data:image/jpeg;base64,abc", dto.PhotoBase64);
        }

        [Fact]
        public void Get_vehicule_mapping_returns_photos_as_data_url()
        {
            var mapper = new MapperConfiguration(
                cfg => cfg.AddProfile<VehiculeMappingProfile>(),
                NullLoggerFactory.Instance).CreateMapper();
            var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };

            var vehicule = new Vehicule
            {
                IdVehicule = 42,
                AliasVehicule = "B1",
                Photos = new List<PhotoVehicule>
                {
                    new()
                    {
                        IdPhotoVehicule = 7,
                        IdVehicule = 42,
                        PhotoData = bytes,
                        Ordre = 1,
                        Statut = true,
                        TypeMIME = "image/jpeg",
                        DateCreation = DateTime.UtcNow
                    },
                    new()
                    {
                        IdPhotoVehicule = 8,
                        IdVehicule = 42,
                        PhotoData = new byte[] { 1 },
                        Ordre = 2,
                        Statut = false,
                        TypeMIME = "image/jpeg",
                        DateCreation = DateTime.UtcNow
                    }
                }
            };

            var dto = mapper.Map<VehiculeResponseDto>(vehicule);

            Assert.Single(dto.Photos);
            Assert.Equal(7, dto.Photos[0].IdPhotoVehicule);
            Assert.StartsWith("data:image/jpeg;base64,", dto.Photos[0].PhotoBase64);
            Assert.Contains(Convert.ToBase64String(bytes), dto.Photos[0].PhotoBase64);
        }

        [Fact]
        public async Task GetByIdAsync_includes_active_photos_for_response_mapping()
        {
            var db = nameof(GetByIdAsync_includes_active_photos_for_response_mapping);
            await using var ctx = new CongoTravelDbContext(
                new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(db).Options);

            var s = new Societe { Nom = "S", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(s);
            await ctx.SaveChangesAsync();

            var tv = new TypeVehicule { Libelle = "T", IdSociete = s.IdSociete, Statut = true, DateCreation = DateTime.UtcNow };
            ctx.TypeVehicules.Add(tv);
            await ctx.SaveChangesAsync();

            var vh = new Vehicule
            {
                AliasVehicule = "V-PHOTO",
                Marques = "M",
                IdTypeVehicule = tv.IdTypeVehicule,
                NombreSiege = 2,
                IdSociete = s.IdSociete,
                NumeroDePlaque = "PH-1",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vh);
            await ctx.SaveChangesAsync();

            ctx.PhotoVehicules.Add(new PhotoVehicule
            {
                IdVehicule = vh.IdVehicule,
                PhotoData = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
                Ordre = 1,
                Statut = true,
                TypeMIME = "image/png",
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var siegeMock = new Mock<ISiegeService>();
            var vehiculeService = new VehiculeService(ctx, NullLogger<VehiculeService>.Instance, siegeMock.Object);
            var mapper = new MapperConfiguration(
                cfg => cfg.AddProfile<VehiculeMappingProfile>(),
                NullLoggerFactory.Instance).CreateMapper();

            var loaded = await vehiculeService.GetByIdAsync(vh.IdVehicule);
            var dto = mapper.Map<VehiculeResponseDto>(loaded!);

            Assert.NotNull(loaded!.Photos);
            Assert.Single(loaded.Photos);
            Assert.Single(dto.Photos);
            Assert.StartsWith("data:image/png;base64,", dto.Photos[0].PhotoBase64);
        }

        [Fact]
        public void Voyage_response_includes_vehicule_photos()
        {
            var mapper = new MapperConfiguration(
                cfg => cfg.AddProfile<VehiculeMappingProfile>(),
                NullLoggerFactory.Instance).CreateMapper();

            var voyage = new Voyage
            {
                Id = 99,
                IdVehicule = 42,
                DateDepart = DateTime.Today,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 100,
                IdDestination = 1,
                IdSociete = 1,
                Statut = true,
                Vehicule = new Vehicule
                {
                    IdVehicule = 42,
                    AliasVehicule = "BUS",
                    Photos = new List<PhotoVehicule>
                    {
                        new()
                        {
                            IdPhotoVehicule = 1,
                            IdVehicule = 42,
                            PhotoData = new byte[] { 0xFF, 0xD8 },
                            Ordre = 1,
                            Statut = true,
                            TypeMIME = "image/jpeg",
                            DateCreation = DateTime.UtcNow
                        }
                    }
                }
            };

            var dto = mapper.Map<VoyageResponseDto>(voyage);

            Assert.Single(dto.PhotosVehicules);
            Assert.StartsWith("data:image/jpeg;base64,", dto.PhotosVehicules[0].PhotoBase64);
            Assert.Equal(42, dto.IdVehicule);
        }
    }
}
