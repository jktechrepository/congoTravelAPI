using AutoMapper;
using CongoTravel.Helpers;
using CongoTravel.Models;

namespace CongoTravel.Models.DTOs.Mapping
{
    public class VehiculeMappingProfile : Profile
    {
        public VehiculeMappingProfile()
        {
            CreateMap<CreateVehiculeDto, Vehicule>()
                .ForMember(dest => dest.IdVehicule, opt => opt.Ignore())
                .ForMember(dest => dest.DateCreation, opt => opt.Ignore())
                .ForMember(dest => dest.DateModification, opt => opt.Ignore())
                .ForMember(dest => dest.Societe, opt => opt.Ignore())
                .ForMember(dest => dest.Photos, opt => opt.Ignore());

            CreateMap<UpdateVehiculeDto, Vehicule>()
                .ForMember(dest => dest.DateCreation, opt => opt.Ignore())
                .ForMember(dest => dest.DateModification, opt => opt.Ignore())
                .ForMember(dest => dest.Societe, opt => opt.Ignore())
                .ForMember(dest => dest.Photos, opt => opt.Ignore());

            CreateMap<Vehicule, VehiculeResponseDto>()
                .ForMember(dest => dest.NomSociete, opt => opt.MapFrom(src => src.Societe != null ? src.Societe.Nom : null))
                .ForMember(dest => dest.LibelleTypeVehicule, opt => opt.MapFrom(src => src.TypeVehicule != null ? src.TypeVehicule.Libelle : null))
                .ForMember(dest => dest.Photos, opt => opt.MapFrom(src =>
                    src.Photos != null
                        ? src.Photos.Where(p => p.Statut).OrderBy(p => p.Ordre)
                        : Enumerable.Empty<PhotoVehicule>()));

            CreateMap<PhotoVehicule, PhotoVehiculeDto>()
                .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(src =>
                    CongoTravelPhotoUrlBuilder.ForVehicule(src.IdVehicule, src.IdPhotoVehicule)))
                .ForMember(dest => dest.PhotoBase64, opt => opt.MapFrom(src => string.Empty));

            CreateMap<CreateTypeVehiculeDto, TypeVehicule>()
                .ForMember(dest => dest.IdTypeVehicule, opt => opt.Ignore())
                .ForMember(dest => dest.DateCreation, opt => opt.Ignore())
                .ForMember(dest => dest.DateModification, opt => opt.Ignore())
                .ForMember(dest => dest.Societe, opt => opt.Ignore());

            CreateMap<UpdateTypeVehiculeDto, TypeVehicule>()
                .ForMember(dest => dest.DateCreation, opt => opt.Ignore())
                .ForMember(dest => dest.DateModification, opt => opt.Ignore())
                .ForMember(dest => dest.Societe, opt => opt.Ignore());

            CreateMap<TypeVehicule, TypeVehiculeResponseDto>();

            CreateMap<CreateVoyageDto, Models.Voyage>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.DateCreation, opt => opt.Ignore())
                .ForMember(dest => dest.DateModification, opt => opt.Ignore())
                .ForMember(dest => dest.Vehicule, opt => opt.Ignore())
                .ForMember(dest => dest.Destination, opt => opt.Ignore())
                .ForMember(dest => dest.Societe, opt => opt.Ignore())
                .ForMember(dest => dest.VoyageDestinations, opt => opt.Ignore())
                .ForMember(dest => dest.VoyageTarifsCategorieSiege, opt => opt.Ignore());

            CreateMap<UpdateVoyageDto, Models.Voyage>()
                .ForMember(dest => dest.DateCreation, opt => opt.Ignore())
                .ForMember(dest => dest.DateModification, opt => opt.Ignore())
                .ForMember(dest => dest.Vehicule, opt => opt.Ignore())
                .ForMember(dest => dest.Destination, opt => opt.Ignore())
                .ForMember(dest => dest.Societe, opt => opt.Ignore())
                .ForMember(dest => dest.VoyageDestinations, opt => opt.Ignore())
                .ForMember(dest => dest.VoyageTarifsCategorieSiege, opt => opt.Ignore());

            CreateMap<Models.Voyage, VoyageResponseDto>()
                .ForMember(dest => dest.EtapesDestinations, opt => opt.Ignore())
                .ForMember(dest => dest.PhotosVehicules, opt => opt.MapFrom(src =>
                    src.Vehicule != null && src.Vehicule.Photos != null
                        ? src.Vehicule.Photos.Where(p => p.Statut).OrderBy(p => p.Ordre)
                        : Enumerable.Empty<PhotoVehicule>()))
                .ForMember(dest => dest.AliasVehicule, opt => opt.MapFrom(src => src.Vehicule != null ? src.Vehicule.AliasVehicule : null))
                .ForMember(dest => dest.LibelleTypeVehicule, opt => opt.MapFrom(src => src.Vehicule != null && src.Vehicule.TypeVehicule != null ? src.Vehicule.TypeVehicule.Libelle : null))
                .ForMember(dest => dest.NomSociete, opt => opt.MapFrom(src => src.Vehicule != null && src.Vehicule.Societe != null ? src.Vehicule.Societe.Nom : null))
                .ForMember(dest => dest.LogoSociete, opt => opt.MapFrom(src => src.Vehicule != null && src.Vehicule.Societe != null ? src.Vehicule.Societe.Logo : null))
                .ForMember(dest => dest.NomSite, opt => opt.MapFrom(src => src.Site != null ? src.Site.NomSite : null))
                .ForMember(dest => dest.VilleDepart, opt => opt.MapFrom(src => src.Destination != null ? src.Destination.VilleDepart : null))
                .ForMember(dest => dest.VilleArrivee, opt => opt.MapFrom(src => src.Destination != null ? src.Destination.VilleArrivee : null));

            CreateMap<CreateReservationDto, Models.Reservation>()
                .ForMember(dest => dest.IdReservation, opt => opt.Ignore())
                .ForMember(dest => dest.Origine, opt => opt.Ignore())
                .ForMember(dest => dest.DateCreation, opt => opt.Ignore())
                .ForMember(dest => dest.DateModification, opt => opt.Ignore())
                .ForMember(dest => dest.Utilisateur, opt => opt.Ignore())
                .ForMember(dest => dest.Client, opt => opt.Ignore())
                .ForMember(dest => dest.Voyage, opt => opt.Ignore())
                .ForMember(dest => dest.Societe, opt => opt.Ignore());

            CreateMap<UpdateReservationDto, Models.Reservation>()
                .ForMember(dest => dest.DateCreation, opt => opt.Ignore())
                .ForMember(dest => dest.DateModification, opt => opt.Ignore())
                .ForMember(dest => dest.Utilisateur, opt => opt.Ignore())
                .ForMember(dest => dest.Client, opt => opt.Ignore())
                .ForMember(dest => dest.Voyage, opt => opt.Ignore())
                .ForMember(dest => dest.Societe, opt => opt.Ignore());

            CreateMap<Models.Reservation, ReservationResponseDto>()
                .ForMember(dest => dest.NomUtilisateur, opt => opt.MapFrom(src => src.Utilisateur != null ? src.Utilisateur.NomComplet : null))
                .ForMember(dest => dest.EmailUtilisateur, opt => opt.MapFrom(src => src.Utilisateur != null ? src.Utilisateur.Email : null))
                .ForMember(dest => dest.NomClient, opt => opt.MapFrom(src => src.Client != null ? src.Client.NomClient : null))
                .ForMember(dest => dest.PrenomClient, opt => opt.Ignore())
                .ForMember(dest => dest.TelephoneClient, opt => opt.MapFrom(src => src.Client != null ? src.Client.Telephone : null))
                .ForMember(dest => dest.DateVoyage, opt => opt.MapFrom(src => src.Voyage != null ? src.Voyage.DateDepart : (DateTime?)null))
                .ForMember(dest => dest.HeureVoyage, opt => opt.MapFrom(src => src.Voyage != null ? src.Voyage.HeureDepart : (TimeSpan?)null))
                .ForMember(dest => dest.PrixVoyage, opt => opt.MapFrom(src => src.Voyage != null ? src.Voyage.Prix : (int?)null))
                .ForMember(dest => dest.AliasVehicule, opt => opt.MapFrom(src => src.Voyage != null && src.Voyage.Vehicule != null ? src.Voyage.Vehicule.AliasVehicule : null))
                .ForMember(dest => dest.VilleDepart, opt => opt.MapFrom(src => src.Voyage != null && src.Voyage.Destination != null ? src.Voyage.Destination.VilleDepart : null))
                .ForMember(dest => dest.VilleArrivee, opt => opt.MapFrom(src => src.Voyage != null && src.Voyage.Destination != null ? src.Voyage.Destination.VilleArrivee : null));

            CreateMap<CreateBilletDto, Billet>()
                .ForMember(dest => dest.IdBillet, opt => opt.Ignore())
                .ForMember(dest => dest.IsUsed, opt => opt.Ignore())
                .ForMember(dest => dest.DateCreation, opt => opt.Ignore())
                .ForMember(dest => dest.DateModification, opt => opt.Ignore())
                .ForMember(dest => dest.Reservation, opt => opt.Ignore())
                .ForMember(dest => dest.Societe, opt => opt.Ignore())
                .ForMember(dest => dest.Site, opt => opt.Ignore());

            CreateMap<UpdateBilletDto, Billet>()
                .ForMember(dest => dest.IsUsed, opt => opt.Ignore())
                .ForMember(dest => dest.DateCreation, opt => opt.Ignore())
                .ForMember(dest => dest.DateModification, opt => opt.Ignore())
                .ForMember(dest => dest.Reservation, opt => opt.Ignore())
                .ForMember(dest => dest.Societe, opt => opt.Ignore())
                .ForMember(dest => dest.Site, opt => opt.Ignore());

            CreateMap<Billet, BilletResponseDto>()
                .ForMember(dest => dest.NomPassager, opt => opt.MapFrom(src => src.ReservationPassenger != null ? src.ReservationPassenger.NomComplet : null))
                .ForMember(dest => dest.StatutReservation, opt => opt.MapFrom(src => src.Reservation != null ? src.Reservation.StatutReservation : null))
                .ForMember(dest => dest.DateReservation, opt => opt.MapFrom(src => src.Reservation != null ? src.Reservation.DateReservation : (DateTime?)null))
                .ForMember(dest => dest.NomUtilisateur, opt => opt.MapFrom(src => src.Reservation != null && src.Reservation.Utilisateur != null ? src.Reservation.Utilisateur.NomComplet : null))
                .ForMember(dest => dest.EmailUtilisateur, opt => opt.MapFrom(src => src.Reservation != null && src.Reservation.Utilisateur != null ? src.Reservation.Utilisateur.Email : null))
                .ForMember(dest => dest.NomClient, opt => opt.MapFrom(src => src.Reservation != null && src.Reservation.Client != null ? src.Reservation.Client.NomClient : null))
                .ForMember(dest => dest.TelephoneClient, opt => opt.MapFrom(src => src.Reservation != null && src.Reservation.Client != null ? src.Reservation.Client.Telephone : null))
                .ForMember(dest => dest.DateVoyage, opt => opt.MapFrom(src => src.Reservation != null && src.Reservation.Voyage != null ? src.Reservation.Voyage.DateDepart : (DateTime?)null))
                .ForMember(dest => dest.HeureVoyage, opt => opt.MapFrom(src => src.Reservation != null && src.Reservation.Voyage != null ? src.Reservation.Voyage.HeureDepart : (TimeSpan?)null))
                .ForMember(dest => dest.PrixVoyage, opt => opt.MapFrom(src => BilletResponseDtoPricing.ResolvePrixVoyage(src)))
                .ForMember(dest => dest.LogoSociete, opt => opt.MapFrom(src => src.Societe != null ? src.Societe.Logo : null))
                .ForMember(dest => dest.AliasVehicule, opt => opt.MapFrom(src => src.Reservation != null && src.Reservation.Voyage != null && src.Reservation.Voyage.Vehicule != null ? src.Reservation.Voyage.Vehicule.AliasVehicule : null))
                .ForMember(dest => dest.VilleDepart, opt => opt.MapFrom(src => src.Reservation != null && src.Reservation.Voyage != null && src.Reservation.Voyage.Destination != null ? src.Reservation.Voyage.Destination.VilleDepart : null))
                .ForMember(dest => dest.VilleArrivee, opt => opt.MapFrom(src => src.Reservation != null && src.Reservation.Voyage != null && src.Reservation.Voyage.Destination != null ? src.Reservation.Voyage.Destination.VilleArrivee : null));
        }
    }
}
