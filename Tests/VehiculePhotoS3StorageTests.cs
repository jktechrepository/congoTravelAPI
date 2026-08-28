using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Mapping;
using CongoTravel.Services.PhotoStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using AutoMapper;
using CongoTravel.Data;
using CongoTravel.Services;
using Xunit;

namespace CongoTravel.Tests
{
    public class VehiculePhotoS3StorageTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static string TinyJpegDataUrl() =>
            "data:image/jpeg;base64," + Convert.ToBase64String(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 });

        private static VehiculePhotoService CreateService(
            CongoTravelDbContext ctx,
            ICongoTravelPhotoBlobStore blobStore)
        {
            var hydrator = PhotoStorageTestFactory.CreateHydrator(blobStore);
            return new VehiculePhotoService(
                ctx,
                blobStore,
                hydrator,
                NullLogger<VehiculePhotoService>.Instance);
        }

        [Fact]
        public void CongoTravelPhotoStorageKeys_combine_prefix_under_congotravel_photos()
        {
            var relative = CongoTravelPhotoStorageKeys.BuildRelativeKey(
                CongoTravelPhotoStorageKeys.EntityVehicules, 12, 1, ".jpg");
            var full = CongoTravelPhotoStorageKeys.CombinePrefix("congotravel/photos", relative);

            Assert.StartsWith("congotravel/photos/vehicules/12/1-", full);
            Assert.EndsWith(".jpg", full);
            Assert.DoesNotContain("devoirs/", full);
        }

        [Fact]
        public async Task AddPhoto_dual_writes_StorageKey_and_keeps_photoBase64_contract()
        {
            await using var ctx = BuildDb(nameof(AddPhoto_dual_writes_StorageKey_and_keeps_photoBase64_contract));
            var societe = new Societe { Nom = "S3 Co", Statut = true, DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var type = new TypeVehicule
            {
                Libelle = "Bus",
                IdSociete = societe.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.TypeVehicules.Add(type);
            await ctx.SaveChangesAsync();

            var vehicule = new Vehicule
            {
                AliasVehicule = "V1",
                IdTypeVehicule = type.IdTypeVehicule,
                IdSociete = societe.IdSociete,
                NombreSiege = 10,
                NumeroDePlaque = "ABC-1",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vehicule);
            await ctx.SaveChangesAsync();

            var blob = PhotoStorageTestFactory.CreateBlobStoreMock();
            var service = CreateService(ctx, blob.Object);

            var photo = await service.AddPhotoAsync(vehicule.IdVehicule, new AddPhotoVehiculeDto
            {
                PhotoBase64 = TinyJpegDataUrl(),
                FileName = "a.jpg",
                Ordre = 1
            });

            Assert.False(string.IsNullOrWhiteSpace(photo.StorageKey));
            Assert.StartsWith("congotravel/photos/vehicules/", photo.StorageKey);
            Assert.NotNull(photo.PhotoData);
            Assert.True(photo.PhotoData!.Length > 0);

            var mapper = new MapperConfiguration(
                cfg => cfg.AddProfile<VehiculeMappingProfile>(),
                NullLoggerFactory.Instance).CreateMapper();

            var dto = mapper.Map<PhotoVehiculeDto>(photo);
            Assert.Equal(
                CongoTravelPhotoUrlBuilder.ForVehicule(photo.IdVehicule, photo.IdPhotoVehicule),
                dto.PhotoUrl);
            Assert.True(string.IsNullOrEmpty(dto.PhotoBase64));

            PhotoContentHelper.ApplyBase64(dto, photo, includePhotoBase64: true);
            Assert.StartsWith("data:image/jpeg;base64,", dto.PhotoBase64);
        }

        [Fact]
        public async Task GetByVehicule_hydrates_from_StorageKey_when_PhotoData_cleared()
        {
            await using var ctx = BuildDb(nameof(GetByVehicule_hydrates_from_StorageKey_when_PhotoData_cleared));
            var societe = new Societe { Nom = "Hydrate Co", Statut = true, DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var type = new TypeVehicule
            {
                Libelle = "Bus",
                IdSociete = societe.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.TypeVehicules.Add(type);
            await ctx.SaveChangesAsync();

            var vehicule = new Vehicule
            {
                AliasVehicule = "V2",
                IdTypeVehicule = type.IdTypeVehicule,
                IdSociete = societe.IdSociete,
                NombreSiege = 10,
                NumeroDePlaque = "ABC-2",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vehicule);
            await ctx.SaveChangesAsync();

            var blob = PhotoStorageTestFactory.CreateBlobStoreMock();
            var service = CreateService(ctx, blob.Object);

            var created = await service.AddPhotoAsync(vehicule.IdVehicule, new AddPhotoVehiculeDto
            {
                PhotoBase64 = TinyJpegDataUrl(),
                FileName = "b.jpg",
                Ordre = 1
            });

            created.PhotoData = null;
            await ctx.SaveChangesAsync();

            var loaded = await service.GetByVehiculeIdAsync(vehicule.IdVehicule, includePhotoBase64: true);
            Assert.Single(loaded);
            Assert.NotNull(loaded[0].PhotoData);
            Assert.True(loaded[0].PhotoData!.Length > 0);

            var mapper = new MapperConfiguration(
                cfg => cfg.AddProfile<VehiculeMappingProfile>(),
                NullLoggerFactory.Instance).CreateMapper();
            var dto = mapper.Map<PhotoVehiculeDto>(loaded[0]);
            Assert.Equal(
                CongoTravelPhotoUrlBuilder.ForVehicule(vehicule.IdVehicule, loaded[0].IdPhotoVehicule),
                dto.PhotoUrl);
            Assert.True(string.IsNullOrEmpty(dto.PhotoBase64));
            PhotoContentHelper.ApplyBase64(dto, loaded[0], includePhotoBase64: true);
            Assert.StartsWith("data:image/jpeg;base64,", dto.PhotoBase64);
        }

        [Fact]
        public async Task GetContent_returns_bytes_from_StorageKey_when_PhotoData_cleared()
        {
            await using var ctx = BuildDb(nameof(GetContent_returns_bytes_from_StorageKey_when_PhotoData_cleared));
            var societe = new Societe { Nom = "Content Co", Statut = true, DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var type = new TypeVehicule
            {
                Libelle = "Bus",
                IdSociete = societe.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.TypeVehicules.Add(type);
            await ctx.SaveChangesAsync();

            var vehicule = new Vehicule
            {
                AliasVehicule = "V4",
                IdTypeVehicule = type.IdTypeVehicule,
                IdSociete = societe.IdSociete,
                NombreSiege = 10,
                NumeroDePlaque = "ABC-4",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vehicule);
            await ctx.SaveChangesAsync();

            var blob = PhotoStorageTestFactory.CreateBlobStoreMock();
            var service = CreateService(ctx, blob.Object);

            var created = await service.AddPhotoAsync(vehicule.IdVehicule, new AddPhotoVehiculeDto
            {
                PhotoBase64 = TinyJpegDataUrl(),
                FileName = "c.jpg",
                Ordre = 1
            });

            created.PhotoData = null;
            await ctx.SaveChangesAsync();

            var payload = await service.GetContentAsync(vehicule.IdVehicule, created.IdPhotoVehicule);
            Assert.NotNull(payload);
            Assert.Equal("image/jpeg", payload!.ContentType);
            Assert.True(payload.Content.Length > 0);

            var missing = await service.GetContentAsync(vehicule.IdVehicule, 99999);
            Assert.Null(missing);
        }

        [Fact]
        public async Task DeletePhoto_removes_blob_object()
        {
            await using var ctx = BuildDb(nameof(DeletePhoto_removes_blob_object));
            var societe = new Societe { Nom = "Del Co", Statut = true, DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var type = new TypeVehicule
            {
                Libelle = "Bus",
                IdSociete = societe.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.TypeVehicules.Add(type);
            await ctx.SaveChangesAsync();

            var vehicule = new Vehicule
            {
                AliasVehicule = "V3",
                IdTypeVehicule = type.IdTypeVehicule,
                IdSociete = societe.IdSociete,
                NombreSiege = 10,
                NumeroDePlaque = "ABC-3",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vehicule);
            await ctx.SaveChangesAsync();

            var blob = PhotoStorageTestFactory.CreateBlobStoreMock();
            var service = CreateService(ctx, blob.Object);

            var created = await service.AddPhotoAsync(vehicule.IdVehicule, new AddPhotoVehiculeDto
            {
                PhotoBase64 = TinyJpegDataUrl(),
                Ordre = 1
            });
            var key = created.StorageKey!;

            var deleted = await service.DeletePhotoAsync(vehicule.IdVehicule, created.IdPhotoVehicule);
            Assert.True(deleted);

            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                blob.Object.GetBytesAsync(key));
        }
    }
}
