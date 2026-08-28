using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace CongoTravel.Tests
{
    public class PhotoUrlTransportTests
    {
        [Fact]
        public void CongoTravelPhotoUrlBuilder_paths_match_controller_routes()
        {
            Assert.Equal("/api/Vehicule/12/photos/5/content", CongoTravelPhotoUrlBuilder.ForVehicule(12, 5));
            Assert.Equal(
                "/api/events/sessions/3/photos/9/content",
                CongoTravelPhotoUrlBuilder.ForEvenementSession(3, 9));
            Assert.Equal(
                "/api/restaurants/etablissements/4/photos/2/content",
                CongoTravelPhotoUrlBuilder.ForRestaurant(4, 2));
            Assert.Equal(
                "/api/sites-touristiques/lieux/7/photos/1/content",
                CongoTravelPhotoUrlBuilder.ForSiteTouristiqueLieu(7, 1));
        }

        [Fact]
        public void ToPhotoDto_default_omits_base64_and_sets_photoUrl()
        {
            var photo = new RestaurantPhoto
            {
                IdRestaurantPhoto = 2,
                IdRestaurant = 4,
                PhotoData = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 },
                TypeMIME = "image/jpeg",
                Ordre = 1,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };

            var dto = RestaurantEtablissementMapper.ToPhotoDto(photo);
            Assert.Equal(CongoTravelPhotoUrlBuilder.ForRestaurant(4, 2), dto.PhotoUrl);
            Assert.True(string.IsNullOrEmpty(dto.PhotoBase64));

            var withBase64 = RestaurantEtablissementMapper.ToPhotoDto(photo, includePhotoBase64: true);
            Assert.Equal(dto.PhotoUrl, withBase64.PhotoUrl);
            Assert.StartsWith("data:image/jpeg;base64,", withBase64.PhotoBase64);
        }

        [Fact]
        public async Task Restaurant_list_cover_uses_photoUrl_without_base64_by_default()
        {
            await using var ctx = new CongoTravelDbContext(
                new DbContextOptionsBuilder<CongoTravelDbContext>()
                    .UseInMemoryDatabase(nameof(Restaurant_list_cover_uses_photoUrl_without_base64_by_default))
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                    .Options);

            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, "Url Co");
            var service = RestaurantTestFactories.CreateEtablissementService(ctx);

            var created = await service.CreateDraftAsync(new RestaurantCreateEtablissementRequestDto
            {
                CodeRestaurant = "URL-1",
                Nom = "Resto URL",
                IdSite = idSite,
                AcomptePourcentDefaut = 10m,
                Photos = new List<AddRestaurantPhotoDto>
                {
                    new()
                    {
                        PhotoBase64 = Convert.ToBase64String(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 }),
                        FileName = "a.jpg",
                        Ordre = 1
                    }
                }
            }, idSociete);
            await service.PublishAsync(created.IdRestaurant, idSociete);

            var list = await service.ListAsync(idSociete);
            var item = Assert.Single(list);
            Assert.NotNull(item.PhotoCouverture);
            Assert.Equal(
                CongoTravelPhotoUrlBuilder.ForRestaurant(created.IdRestaurant, item.PhotoCouverture!.IdRestaurantPhoto),
                item.PhotoCouverture.PhotoUrl);
            Assert.True(string.IsNullOrEmpty(item.PhotoCouverture.PhotoBase64));

            var withBase64 = await service.ListAsync(idSociete, includePhotoBase64: true);
            Assert.StartsWith("data:image/jpeg;base64,", withBase64[0].PhotoCouverture!.PhotoBase64);
        }
    }
}
