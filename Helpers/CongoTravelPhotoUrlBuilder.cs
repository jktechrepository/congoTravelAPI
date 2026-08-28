namespace CongoTravel.Helpers
{
    /// <summary>Chemins relatifs stables pour streamer les photos via l'API (photoUrl).</summary>
    public static class CongoTravelPhotoUrlBuilder
    {
        public static string ForVehicule(int idVehicule, int idPhotoVehicule) =>
            $"/api/Vehicule/{idVehicule}/photos/{idPhotoVehicule}/content";

        public static string ForEvenementSession(int idEvenementSession, int idEvenementSessionPhoto) =>
            $"/api/events/sessions/{idEvenementSession}/photos/{idEvenementSessionPhoto}/content";

        public static string ForRestaurant(int idRestaurant, int idRestaurantPhoto) =>
            $"/api/restaurants/etablissements/{idRestaurant}/photos/{idRestaurantPhoto}/content";

        public static string ForHotel(int idHotel, int idHotelPhoto) =>
            $"/api/hotels/etablissements/{idHotel}/photos/{idHotelPhoto}/content";

        public static string ForSiteTouristiqueLieu(int idSiteTouristique, int idSiteTouristiqueLieuPhoto) =>
            $"/api/sites-touristiques/lieux/{idSiteTouristique}/photos/{idSiteTouristiqueLieuPhoto}/content";
    }
}
