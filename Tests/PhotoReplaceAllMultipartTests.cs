using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CongoTravel.Tests
{
    public class PhotoReplaceAllMultipartTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static IFormFile FormFile(byte[] content, string fileName, string contentType = "image/jpeg")
        {
            var stream = new MemoryStream(content);
            return new FormFile(stream, 0, content.Length, "files", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        private static byte[] TinyJpeg(byte marker = 0xD9) => new byte[] { 0xFF, 0xD8, 0xFF, marker };

        private static VehiculePhotoService CreateService(CongoTravelDbContext ctx)
        {
            var mock = PhotoStorageTestFactory.CreateBlobStoreMock();
            return new VehiculePhotoService(
                ctx,
                mock.Object,
                PhotoStorageTestFactory.CreateHydrator(mock.Object),
                NullLogger<VehiculePhotoService>.Instance);
        }

        private static async Task<Vehicule> SeedVehiculeAsync(CongoTravelDbContext ctx, string plate = "RA-1")
        {
            var societe = new Societe { Nom = "RA Co", Statut = true, DateCreation = DateTime.UtcNow };
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
                AliasVehicule = "V-RA",
                IdTypeVehicule = type.IdTypeVehicule,
                IdSociete = societe.IdSociete,
                NombreSiege = 10,
                NumeroDePlaque = plate,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vehicule);
            await ctx.SaveChangesAsync();
            return vehicule;
        }

        [Fact]
        public async Task ReplaceAllFromFiles_with_two_files_replaces_gallery()
        {
            await using var ctx = BuildDb(nameof(ReplaceAllFromFiles_with_two_files_replaces_gallery));
            var vehicule = await SeedVehiculeAsync(ctx);
            var service = CreateService(ctx);

            await service.AddPhotoFromFileAsync(
                vehicule.IdVehicule,
                FormFile(TinyJpeg(0xD1), "old.jpg"),
                ordre: 1);

            var result = await service.ReplaceAllFromFilesAsync(
                vehicule.IdVehicule,
                new List<IFormFile>
                {
                    FormFile(TinyJpeg(0xD2), "a.jpg"),
                    FormFile(TinyJpeg(0xD3), "b.jpg")
                },
                new List<int> { 1, 2 });

            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.False(string.IsNullOrWhiteSpace(p.StorageKey)));
            Assert.Equal(2, await ctx.PhotoVehicules.CountAsync(p => p.IdVehicule == vehicule.IdVehicule));
        }

        [Fact]
        public async Task ReplaceAllFromFiles_with_empty_list_clears_gallery()
        {
            await using var ctx = BuildDb(nameof(ReplaceAllFromFiles_with_empty_list_clears_gallery));
            var vehicule = await SeedVehiculeAsync(ctx, "RA-2");
            var service = CreateService(ctx);

            await service.AddPhotoFromFileAsync(
                vehicule.IdVehicule,
                FormFile(TinyJpeg(), "keep-me-not.jpg"));

            var result = await service.ReplaceAllFromFilesAsync(
                vehicule.IdVehicule,
                Array.Empty<IFormFile>());

            Assert.Empty(result);
            Assert.Equal(0, await ctx.PhotoVehicules.CountAsync(p => p.IdVehicule == vehicule.IdVehicule));
        }

        [Fact]
        public async Task ReplaceAllFromFiles_rejects_four_files()
        {
            await using var ctx = BuildDb(nameof(ReplaceAllFromFiles_rejects_four_files));
            var vehicule = await SeedVehiculeAsync(ctx, "RA-3");
            var service = CreateService(ctx);

            var files = Enumerable.Range(0, 4)
                .Select(i => FormFile(TinyJpeg((byte)(0xD0 + i)), $"{i}.jpg"))
                .ToList();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReplaceAllFromFilesAsync(vehicule.IdVehicule, files));
        }

        [Fact]
        public async Task Create_without_photos_then_multipart_add_works()
        {
            await using var ctx = BuildDb(nameof(Create_without_photos_then_multipart_add_works));
            var vehicule = await SeedVehiculeAsync(ctx, "RA-4");
            var service = CreateService(ctx);

            await service.AddPhotosOnCreateAsync(vehicule.IdVehicule, null);
            Assert.Equal(0, await ctx.PhotoVehicules.CountAsync());

            var photo = await service.AddPhotoFromFileAsync(
                vehicule.IdVehicule,
                FormFile(TinyJpeg(), "after-create.jpg"),
                ordre: 1);

            Assert.Equal(
                CongoTravelPhotoUrlBuilder.ForVehicule(vehicule.IdVehicule, photo.IdPhotoVehicule),
                CongoTravelPhotoUrlBuilder.ForVehicule(photo.IdVehicule, photo.IdPhotoVehicule));
            Assert.False(string.IsNullOrWhiteSpace(photo.StorageKey));
        }

        [Fact]
        public async Task Create_with_legacy_base64_photos_still_works()
        {
            await using var ctx = BuildDb(nameof(Create_with_legacy_base64_photos_still_works));
            var vehicule = await SeedVehiculeAsync(ctx, "RA-5");
            var service = CreateService(ctx);

            await service.AddPhotosOnCreateAsync(vehicule.IdVehicule, new List<AddPhotoVehiculeDto>
            {
                new()
                {
                    PhotoBase64 = "data:image/jpeg;base64," + Convert.ToBase64String(TinyJpeg()),
                    FileName = "legacy.jpg",
                    Ordre = 1
                }
            });

            Assert.Equal(1, await ctx.PhotoVehicules.CountAsync(p => p.IdVehicule == vehicule.IdVehicule));
        }
    }
}
