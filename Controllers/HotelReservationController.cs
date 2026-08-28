using CongoTravel.Attributes;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;
using CongoTravel.Models.Hotel.Enums;
using CongoTravel.Services.Hotel;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CongoTravel.Controllers
{
    [ApiController, Route("api/hotels/reservations"), Authorize]
    public class HotelReservationController : ControllerBase
    {
        private readonly IHotelReservationService _reservations;
        private readonly IHotelReservationWithPaiementService _withPayment;
        private readonly ICurrentUserService _currentUser;
        public HotelReservationController(IHotelReservationService reservations,
            IHotelReservationWithPaiementService withPayment, ICurrentUserService currentUser)
        {
            _reservations = reservations; _withPayment = withPayment; _currentUser = currentUser;
        }

        [HttpPost("with-paiement")]
        [Permission("Hotel.Hold.Create"), Permission("Hotel.Reservation.Confirm")]
        public async Task<ActionResult<HotelReservationWithPaiementResponseDto>> Create(
            HotelReservationWithPaiementRequestDto request, CancellationToken cancellationToken)
        {
            try { return Ok(await _withPayment.CreateCashAsync(request, cancellationToken)); }
            catch (HotelHoldConflictException ex) { return Conflict(new { message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("with-paiement-electronique")]
        [Permission("Hotel.Hold.Create"), Permission("Hotel.Reservation.Confirm")]
        public async Task<ActionResult<HotelReservationWithPaiementResponseDto>> CreateElectronic(
            HotelReservationWithPaiementRequestDto request, CancellationToken cancellationToken)
        {
            try { return Ok(await _withPayment.InitiateElectronicAsync(request, cancellationToken)); }
            catch (HotelHoldConflictException ex) { return Conflict(new { message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet, Permission("Hotel.Etablissement.Read")]
        public async Task<ActionResult<IEnumerable<HotelReservationListItemDto>>> List(
            [FromQuery] int? idSociete, [FromQuery] string? status,
            [FromQuery] int? idHotel, CancellationToken cancellationToken)
        {
            try
            {
                var societe = HotelTenancyGuard.ResolveEffectiveSocieteIdForFlexPayVerifier(
                    _currentUser, idSociete);
                HotelReservationStatus? parsed = HotelReservationStatus.CONFIRMED;
                if (string.Equals(status, "ALL", StringComparison.OrdinalIgnoreCase)) parsed = null;
                else if (!string.IsNullOrWhiteSpace(status))
                {
                    if (!Enum.TryParse<HotelReservationStatus>(status, true, out var value))
                        return BadRequest(new { message = "Statut hôtel invalide." });
                    parsed = value;
                }
                var filter = new HotelReservationListFilter { Status = parsed, IdHotel = idHotel };
                HotelTenancyGuard.ApplyClientSelfScopeToListFilter(_currentUser, filter);
                return Ok(await _reservations.ListAsync(societe, filter, cancellationToken));
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpGet("client/{idClient:int}"), Permission("Hotel.Etablissement.Read")]
        public async Task<ActionResult<IEnumerable<HotelReservationListItemDto>>> ListByClient(
            int idClient, [FromQuery] string? status, CancellationToken cancellationToken)
        {
            try
            {
                HotelTenancyGuard.EnsureClientMayQueryByClientId(_currentUser, idClient);
                HotelReservationStatus? parsed = HotelReservationStatus.CONFIRMED;
                if (string.Equals(status, "ALL", StringComparison.OrdinalIgnoreCase)) parsed = null;
                else if (!string.IsNullOrWhiteSpace(status))
                {
                    if (!Enum.TryParse<HotelReservationStatus>(status, true, out var value))
                        return BadRequest(new { message = "Statut hôtel invalide." });
                    parsed = value;
                }

                return Ok(await _reservations.ListByClientAsync(
                    idClient,
                    new HotelReservationListFilter { Status = parsed },
                    cancellationToken));
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpGet("{id:int}"), Permission("Hotel.Etablissement.Read")]
        public async Task<ActionResult<HotelReservationResponseDto>> Detail(
            int id, [FromQuery] int? idSociete, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _reservations.GetByIdAsync(
                    id,
                    HotelTenancyGuard.ResolveEffectiveSocieteIdForFlexPayVerifier(_currentUser, idSociete),
                    cancellationToken);
                if (result == null) return NotFound();
                HotelTenancyGuard.EnsureClientOwnsReservation(
                    _currentUser, result.IdUtilisateur, result.IdClient);
                return Ok(result);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("{id:int}/cancel"), Permission("Hotel.Reservation.Confirm")]
        public async Task<ActionResult<HotelCancelReservationResponseDto>> Cancel(
            int id, [FromQuery] int? idSociete, CancellationToken cancellationToken)
        {
            try
            {
                var societe = HotelTenancyGuard.ResolveEffectiveSocieteIdForFlexPayVerifier(
                    _currentUser, idSociete);
                var existing = await _reservations.GetByIdAsync(id, societe, cancellationToken);
                if (existing == null) return NotFound();
                HotelTenancyGuard.EnsureClientOwnsReservation(
                    _currentUser, existing.IdUtilisateur, existing.IdClient);
                return Ok(await _reservations.CancelAsync(id, societe, cancellationToken));
            }
            catch (HotelHoldConflictException ex) { return Conflict(new { message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("{id:int}/assign-rooms"), HttpPut("{id:int}/assign-rooms")]
        [Permission("Hotel.Etablissement.Write")]
        public async Task<ActionResult<HotelReservationResponseDto>> AssignRooms(
            int id,
            HotelAssignRoomsRequestDto request,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var societe = HotelTenancyGuard.ResolveEffectiveSocieteIdForFlexPayVerifier(
                    _currentUser, idSociete);
                return Ok(await _reservations.AssignRoomsAsync(id, societe, request, cancellationToken));
            }
            catch (HotelRoomAssignmentConflictException ex) { return Conflict(new { message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("{id:int}/check-in"), HttpPut("{id:int}/check-in")]
        [Permission("Hotel.Etablissement.Write")]
        public async Task<ActionResult<HotelReservationResponseDto>> CheckIn(
            int id,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var societe = HotelTenancyGuard.ResolveEffectiveSocieteIdForFlexPayVerifier(
                    _currentUser, idSociete);
                return Ok(await _reservations.CheckInAsync(id, societe, cancellationToken));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("{id:int}/check-out"), HttpPut("{id:int}/check-out")]
        [Permission("Hotel.Etablissement.Write")]
        public async Task<ActionResult<HotelReservationResponseDto>> CheckOut(
            int id,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var societe = HotelTenancyGuard.ResolveEffectiveSocieteIdForFlexPayVerifier(
                    _currentUser, idSociete);
                return Ok(await _reservations.CheckOutAsync(id, societe, cancellationToken));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("{id:int}/extras"), HttpPut("{id:int}/extras")]
        [Permission("Hotel.Etablissement.Write")]
        public async Task<ActionResult<HotelReservationResponseDto>> SetExtras(
            int id,
            HotelSetReservationExtrasRequestDto request,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var societe = HotelTenancyGuard.ResolveEffectiveSocieteIdForFlexPayVerifier(
                    _currentUser, idSociete);
                return Ok(await _reservations.SetExtrasAsync(id, societe, request, cancellationToken));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

    }
}
