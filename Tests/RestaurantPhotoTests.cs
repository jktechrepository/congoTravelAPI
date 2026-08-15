using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CongoTravel.Data;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Services.Restaurant;
using Xunit;

namespace CongoTravel.Tests
{
    public class RestaurantPhotoTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static string TinyJpegBase64() =>
            Convert.ToBase64String(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 });

        private static AddRestaurantPhotoDto PhotoDto(int? ordre = null, string fileName = "cover.jpg") =>
            new()
            {
                PhotoBase64 = TinyJpegBase64(),
                FileName = fileName,
                Ordre = ordre
            };

        [Fact]
        public async Task CreateDraftAsync_with_photos_returns_photos_and_cover()
        {
            await using var ctx = BuildDb(nameof(CreateDraftAsync_with_photos_returns_photos_and_cover));
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, "Resto Photo Co");
            var service = RestaurantTestFactories.CreateEtablissementService(ctx);

            var created = await service.CreateDraftAsync(new RestaurantCreateEtablissementRequestDto
            {
                CodeRestaurant = "REST-PICS",
                Nom = "Restaurant avec photos",
                IdSite = idSite,
                AcomptePourcentDefaut = 20m,
                Photos = new List<AddRestaurantPhotoDto>
                {
                    PhotoDto(1, "cover.jpg"),
                    PhotoDto(2, "second.jpg")
                }
            }, idSociete);

            Assert.NotNull(created.PhotoCouverture);
            Assert.Equal(1, created.PhotoCouverture!.Ordre);
            Assert.StartsWith("data:image/jpeg;base64,", created.PhotoCouverture.PhotoBase64);
            Assert.Equal(2, created.Photos.Count);
            Assert.Equal(1, created.Photos[0].Ordre);
            Assert.Equal(2, created.Photos[1].Ordre);

            var detail = await service.GetByIdAsync(created.IdRestaurant, idSociete);
            Assert.NotNull(detail);
            Assert.Equal(2, detail!.Photos.Count);
            Assert.NotNull(detail.PhotoCouverture);
            Assert.Equal(1, detail.PhotoCouverture!.Ordre);
        }

        [Fact]
        public async Task CreateDraftAsync_without_photos_has_null_cover_and_empty_photos()
        {
            await using var ctx = BuildDb(nameof(CreateDraftAsync_without_photos_has_null_cover_and_empty_photos));
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, "Resto No Pic");
            var service = RestaurantTestFactories.CreateEtablissementService(ctx);

            var created = await service.CreateDraftAsync(new RestaurantCreateEtablissementRequestDto
            {
                CodeRestaurant = "NO-PIC",
                Nom = "Sans photo",
                IdSite = idSite,
                AcomptePourcentDefaut = 10m
            }, idSociete);

            Assert.Null(created.PhotoCouverture);
            Assert.Empty(created.Photos);
        }

        [Fact]
        public async Task ListAsync_enriches_cover_photo()
        {
            await using var ctx = BuildDb(nameof(ListAsync_enriches_cover_photo));
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, "Resto List Photo");
            var service = RestaurantTestFactories.CreateEtablissementService(ctx);

            var created = await service.CreateDraftAsync(new RestaurantCreateEtablissementRequestDto
            {
                CodeRestaurant = "LIST-PIC",
                Nom = "Catalogue photo",
                IdSite = idSite,
                AcomptePourcentDefaut = 15m,
                Photos = new List<AddRestaurantPhotoDto> { PhotoDto(1) }
            }, idSociete);
            await service.PublishAsync(created.IdRestaurant, idSociete);

            var list = await service.ListAsync(idSociete);
            var item = Assert.Single(list);
            Assert.NotNull(item.PhotoCouverture);
            Assert.StartsWith("data:image/jpeg;base64,", item.PhotoCouverture!.PhotoBase64);
            Assert.Equal(1, item.PhotoCouverture.Ordre);
        }

        [Fact]
        public async Task AddPhotoAsync_allows_up_to_three_photos()
        {
            await using var ctx = BuildDb(nameof(AddPhotoAsync_allows_up_to_three_photos));
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx);
            var etablissementService = RestaurantTestFactories.CreateEtablissementService(ctx);
            var photoService = RestaurantTestFactories.CreatePhotoService(ctx);

            var resto = await etablissementService.CreateDraftAsync(new RestaurantCreateEtablissementRequestDto
            {
                CodeRestaurant = "ADD-3",
                Nom = "Trois photos",
                IdSite = idSite,
                AcomptePourcentDefaut = 0m
            }, idSociete);

            await photoService.AddPhotoAsync(resto.IdRestaurant, idSociete, PhotoDto(1));
            await photoService.AddPhotoAsync(resto.IdRestaurant, idSociete, PhotoDto(2));
            await photoService.AddPhotoAsync(resto.IdRestaurant, idSociete, PhotoDto(3));

            var list = await photoService.GetByRestaurantIdAsync(resto.IdRestaurant, idSociete);
            Assert.Equal(3, list.Count);
        }

        [Fact]
        public async Task AddPhotoAsync_rejects_fourth_photo()
        {
            await using var ctx = BuildDb(nameof(AddPhotoAsync_rejects_fourth_photo));
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx);
            var etablissementService = RestaurantTestFactories.CreateEtablissementService(ctx);
            var photoService = RestaurantTestFactories.CreatePhotoService(ctx);

            var resto = await etablissementService.CreateDraftAsync(new RestaurantCreateEtablissementRequestDto
            {
                CodeRestaurant = "ADD-4",
                Nom = "Trop de photos",
                IdSite = idSite,
                AcomptePourcentDefaut = 0m
            }, idSociete);

            await photoService.AddPhotoAsync(resto.IdRestaurant, idSociete, PhotoDto());
            await photoService.AddPhotoAsync(resto.IdRestaurant, idSociete, PhotoDto());
            await photoService.AddPhotoAsync(resto.IdRestaurant, idSociete, PhotoDto());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                photoService.AddPhotoAsync(resto.IdRestaurant, idSociete, PhotoDto()));
        }

        [Fact]
        public async Task DeletePhotoAsync_removes_photo()
        {
            await using var ctx = BuildDb(nameof(DeletePhotoAsync_removes_photo));
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx);
            var etablissementService = RestaurantTestFactories.CreateEtablissementService(ctx);
            var photoService = RestaurantTestFactories.CreatePhotoService(ctx);

            var resto = await etablissementService.CreateDraftAsync(new RestaurantCreateEtablissementRequestDto
            {
                CodeRestaurant = "DEL-PIC",
                Nom = "Suppression photo",
                IdSite = idSite,
                AcomptePourcentDefaut = 0m,
                Photos = new List<AddRestaurantPhotoDto> { PhotoDto(1) }
            }, idSociete);

            var photos = await photoService.GetByRestaurantIdAsync(resto.IdRestaurant, idSociete);
            var photoId = Assert.Single(photos).IdRestaurantPhoto;

            var deleted = await photoService.DeletePhotoAsync(resto.IdRestaurant, idSociete, photoId);
            Assert.True(deleted);

            var remaining = await photoService.GetByRestaurantIdAsync(resto.IdRestaurant, idSociete);
            Assert.Empty(remaining);
        }
    }
}
