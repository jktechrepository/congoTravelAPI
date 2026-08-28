using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using Xunit;

namespace CongoTravel.Tests
{
    public class PhotoMultipartUploadTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static IFormFile FormFile(byte[] content, string fileName, string contentType)
        {
            var stream = new MemoryStream(content);
            return new FormFile(stream, 0, content.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        private static byte[] TinyJpeg() => new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 };

        private static VehiculePhotoService CreateService(CongoTravelDbContext ctx)
        {
            var blob = PhotoStorageTestFactory.CreateBlobStoreMock();
            return new VehiculePhotoService(
                ctx,
                blob.Object,
                PhotoStorageTestFactory.CreateHydrator(blob.Object),
                NullLogger<VehiculePhotoService>.Instance);
        }

        private static async Task<Vehicule> SeedVehiculeAsync(CongoTravelDbContext ctx)
        {
            var societe = new Societe { Nom = "MP Co", Statut = true, DateCreation = DateTime.UtcNow };
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
                AliasVehicule = "V-MP",
                IdTypeVehicule = type.IdTypeVehicule,
                IdSociete = societe.IdSociete,
                NombreSiege = 10,
                NumeroDePlaque = "MP-1",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vehicule);
            await ctx.SaveChangesAsync();
            return vehicule;
        }

        [Fact]
        public async Task ParseAndValidateFile_rejects_empty_and_oversized_and_bad_type()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                VehiculePhotoBase64Helper.ParseAndValidateFileAsync(
                    FormFile(Array.Empty<byte>(), "a.jpg", "image/jpeg")));

            var oversized = new byte[VehiculePhotoBase64Helper.MaxImageBytes + 1];
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                VehiculePhotoBase64Helper.ParseAndValidateFileAsync(
                    FormFile(oversized, "big.jpg", "image/jpeg")));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                VehiculePhotoBase64Helper.ParseAndValidateFileAsync(
                    FormFile(TinyJpeg(), "a.gif", "image/gif")));
        }

        [Fact]
        public async Task ParseAndValidateFile_accepts_jpeg()
        {
            var (bytes, ext, contentType) = await VehiculePhotoBase64Helper.ParseAndValidateFileAsync(
                FormFile(TinyJpeg(), "cover.jpg", "image/jpeg"));

            Assert.Equal(TinyJpeg(), bytes);
            Assert.Equal(".jpg", ext);
            Assert.Equal("image/jpeg", contentType);
        }

        [Fact]
        public async Task AddPhotoFromFile_dual_writes_StorageKey_and_sets_photoUrl_contract()
        {
            await using var ctx = BuildDb(nameof(AddPhotoFromFile_dual_writes_StorageKey_and_sets_photoUrl_contract));
            var vehicule = await SeedVehiculeAsync(ctx);
            var service = CreateService(ctx);

            var photo = await service.AddPhotoFromFileAsync(
                vehicule.IdVehicule,
                FormFile(TinyJpeg(), "a.jpg", "image/jpeg"),
                ordre: 1);

            Assert.False(string.IsNullOrWhiteSpace(photo.StorageKey));
            Assert.StartsWith("congotravel/photos/vehicules/", photo.StorageKey);
            Assert.NotNull(photo.PhotoData);
            Assert.Equal(1, photo.Ordre);

            var jsonPhoto = await service.AddPhotoAsync(vehicule.IdVehicule, new AddPhotoVehiculeDto
            {
                PhotoBase64 = "data:image/jpeg;base64," + Convert.ToBase64String(TinyJpeg()),
                FileName = "b.jpg",
                Ordre = 2
            });
            Assert.False(string.IsNullOrWhiteSpace(jsonPhoto.StorageKey));
            Assert.Equal(2, jsonPhoto.Ordre);

            Assert.Equal(
                CongoTravelPhotoUrlBuilder.ForVehicule(vehicule.IdVehicule, photo.IdPhotoVehicule),
                CongoTravelPhotoUrlBuilder.ForVehicule(photo.IdVehicule, photo.IdPhotoVehicule));
        }

        [Fact]
        public async Task AddPhotoFromFile_rejects_fourth_photo()
        {
            await using var ctx = BuildDb(nameof(AddPhotoFromFile_rejects_fourth_photo));
            var vehicule = await SeedVehiculeAsync(ctx);
            var service = CreateService(ctx);

            await service.AddPhotoFromFileAsync(vehicule.IdVehicule, FormFile(TinyJpeg(), "1.jpg", "image/jpeg"));
            await service.AddPhotoFromFileAsync(vehicule.IdVehicule, FormFile(TinyJpeg(), "2.jpg", "image/jpeg"));
            await service.AddPhotoFromFileAsync(vehicule.IdVehicule, FormFile(TinyJpeg(), "3.jpg", "image/jpeg"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AddPhotoFromFileAsync(vehicule.IdVehicule, FormFile(TinyJpeg(), "4.jpg", "image/jpeg")));
        }
    }
}
