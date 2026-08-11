using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantEtablissementListFilter
    {
        public RestaurantStatus? Status { get; set; }
        public int? IdSociete { get; set; }
    }
}
