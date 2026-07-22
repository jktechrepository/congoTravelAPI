using AutoMapper;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Reservation;

namespace CongoTravel.Models.DTOs.Mapping
{
    /// <summary>
    /// Mapping lecture workflow réservation V2 (étapes voyage, sièges libres, passagers).
    /// </summary>
    public class WorkflowReservationMappingProfile : Profile
    {
        public WorkflowReservationMappingProfile()
        {
            CreateMap<ReservationPassenger, ReservationPassengerReadDto>();

            CreateMap<Siege, SiegeLibreReadDto>();

            CreateMap<VoyageDestination, VoyageEtapeReadDto>()
                .ForMember(d => d.VilleDepart,
                    o => o.MapFrom(s => s.Destination != null ? s.Destination.VilleDepart : string.Empty))
                .ForMember(d => d.VilleArrivee,
                    o => o.MapFrom(s => s.Destination != null ? s.Destination.VilleArrivee : string.Empty));

            CreateMap<VoyageSeatAllocation, SiegeIndisponibleReadDto>()
                .ForMember(d => d.NumeroOrdre,
                    o => o.MapFrom(a => a.Siege != null ? a.Siege.NumeroOrdre : 0))
                .ForMember(d => d.CodeSiege,
                    o => o.MapFrom(a => a.Siege != null ? a.Siege.CodeSiege : string.Empty))
                .ForMember(d => d.IdVoyageSeatAllocation,
                    o => o.MapFrom(a => a.IdVoyageSeatAllocation))
                .ForMember(d => d.NomPassager,
                    o => o.MapFrom(a =>
                        a.ReservationPassenger != null ? a.ReservationPassenger.NomComplet : string.Empty));
        }
    }
}
