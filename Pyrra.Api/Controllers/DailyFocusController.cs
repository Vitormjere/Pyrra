using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pyrra.Api.Dtos.Focos;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Focos;

namespace Pyrra.Api.Controllers {
    [ApiController]
    [Authorize]
    [Route("api/focos")]
    public class DailyFocusController : ControllerBase {
        private readonly IDailyFocusService _focusService;
        private readonly IFocusCheckInService _checkInService;

        public DailyFocusController(IDailyFocusService focusService, IFocusCheckInService checkInService) {
            _focusService   = focusService;
            _checkInService = checkInService;
        }

        [HttpPost]
        public async Task<ActionResult<FocusResponse>> Create(CreateFocusRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var focus = await _focusService.CreateAsync(userId, request.Name, cancellationToken);
                var response = FocusResponse.FromEntity(focus);
                return Created($"/api/focos/{focus.Id}", response);
            } catch (DuplicateFocusException ex) {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FocusResponse>>> GetAll(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var focuses = await _focusService.GetAllForUserAsync(userId, cancellationToken);
            return Ok(focuses.Select(FocusResponse.FromEntity));
        }

        [HttpPatch("{id:guid}/peso")]
        public async Task<ActionResult<FocusResponse>> UpdateWeight(Guid id, UpdateWeightRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var focus = await _focusService.UpdateWeightAsync(userId, id, request.Weight, cancellationToken);
                return Ok(FocusResponse.FromEntity(focus));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _focusService.DeactivateAsync(userId, id, cancellationToken);
                return NoContent();
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{id:guid}/checkin")]
        public async Task<ActionResult<DailyScoreResponse>> ToggleCheckIn(Guid id, [FromQuery] DateOnly? date, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var result = await _checkInService.ToggleCheckInAsync(userId, id, date, cancellationToken);
                return Ok(DailyScoreResponse.FromResult(result));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            } catch (FutureDateException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (PastCheckInDateException ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("score")]
        public async Task<ActionResult<DailyScoreResponse>> GetScore([FromQuery] DateOnly? date, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var result = await _checkInService.GetDailyScoreAsync(userId, date, cancellationToken);
                return Ok(DailyScoreResponse.FromResult(result));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            } catch (FutureDateException ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        // O userId vem SEMPRE do token (claim NameIdentifier), nunca do corpo da requisição,
        // impedindo que um usuário manipule focos de outro passando outro id no payload.
        private bool TryGetUserId(out Guid userId) {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out userId);
        }
    }
}
