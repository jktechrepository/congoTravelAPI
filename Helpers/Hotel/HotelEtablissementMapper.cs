using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;
using HotelEntity = CongoTravel.Models.Hotel.Hotel;

namespace CongoTravel.Helpers.Hotel
{
    public static class HotelEtablissementMapper
    {
        public static HotelEtablissementListItemDto ToListItemDto(HotelEntity hotel, bool includePhotoBase64 = false) =>
            new()
            {
                IdHotel = hotel.IdHotel, IdSociete = hotel.IdSociete, NomSociete = hotel.Societe?.Nom,
                IdSite = hotel.IdSite, NomSite = hotel.Site?.NomSite, CodeHotel = hotel.CodeHotel,
                Nom = hotel.Nom, Description = hotel.Description, Adresse = hotel.Adresse,
                AcomptePourcentDefaut = hotel.AcomptePourcentDefaut, Status = hotel.Status.ToString(),
                DateCreation = hotel.DateCreation, DateModification = hotel.DateModification,
                PhotoCouverture = ResolveCoverPhoto(hotel, includePhotoBase64)
            };

        public static HotelEtablissementResponseDto ToResponseDto(HotelEntity hotel, bool includePhotoBase64 = false) =>
            new()
            {
                IdHotel = hotel.IdHotel, IdSociete = hotel.IdSociete, NomSociete = hotel.Societe?.Nom,
                IdSite = hotel.IdSite, NomSite = hotel.Site?.NomSite, CodeHotel = hotel.CodeHotel,
                Nom = hotel.Nom, Description = hotel.Description, Adresse = hotel.Adresse,
                AcomptePourcentDefaut = hotel.AcomptePourcentDefaut, Status = hotel.Status.ToString(),
                DateCreation = hotel.DateCreation, DateModification = hotel.DateModification,
                RoomTypesCount = hotel.RoomTypes?.Count ?? 0,
                PhotoCouverture = ResolveCoverPhoto(hotel, includePhotoBase64),
                Photos = hotel.Photos?.Where(p => p.Statut).OrderBy(p => p.Ordre)
                    .Select(p => ToPhotoDto(p, includePhotoBase64)).ToList() ?? new()
            };

        public static HotelPhotoDto ToPhotoDto(HotelPhoto photo, bool includePhotoBase64 = false) =>
            new()
            {
                IdHotelPhoto = photo.IdHotelPhoto, IdHotel = photo.IdHotel,
                PhotoUrl = CongoTravelPhotoUrlBuilder.ForHotel(photo.IdHotel, photo.IdHotelPhoto),
                PhotoBase64 = PhotoContentHelper.EncodeBase64IfRequested(photo.PhotoData, photo.TypeMIME, includePhotoBase64),
                Ordre = photo.Ordre, OriginalFileName = photo.OriginalFileName, TypeMIME = photo.TypeMIME,
                FileSize = photo.FileSize, Statut = photo.Statut, DateCreation = photo.DateCreation,
                DateModification = photo.DateModification
            };

        private static HotelPhotoDto? ResolveCoverPhoto(HotelEntity hotel, bool includePhotoBase64) =>
            hotel.Photos?.Where(p => p.Statut).OrderBy(p => p.Ordre).Select(p => ToPhotoDto(p, includePhotoBase64)).FirstOrDefault();
    }
}
