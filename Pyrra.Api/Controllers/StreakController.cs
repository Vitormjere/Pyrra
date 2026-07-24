using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pyrra.Api.Dtos.Streaks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Streaks;

namespace Pyrra.Api.Controllers {
    [ApiController]
    [Authorize]
    [Route("api/streak")]
    public class StreakController : ControllerBase {
        private readonly IStreakService _streakService;

        public StreakController(IStreakService streakService) {
            _streakService = streakService;
        }

        [HttpGet]
        public async Task<ActionResult<StreakStatusResponse>> GetStatus(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var status = await _streakService.GetStatusAsync(userId, cancellationToken);
                return Ok(StreakStatusResponse.FromResult(status));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("marcos-pendentes")]
        public async Task<ActionResult<IEnumerable<PendingMilestoneResponse>>> GetPendingMilestones(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var pending = await _streakService.GetPendingMilestonesAsync(userId, cancellationToken);
                return Ok(pending.Select(PendingMilestoneResponse.FromResult));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("marcos-pendentes/confirmar")]
        public async Task<ActionResult<AcknowledgeMilestonesResponse>> AcknowledgeMilestones(
            [FromBody] AcknowledgeMilestonesRequest? request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var acknowledged = await _streakService.AcknowledgeMilestonesAsync(userId, request?.Ids, cancellationToken);
            return Ok(new AcknowledgeMilestonesResponse(acknowledged));
        }

        [HttpGet("freezes-usados-pendentes")]
        public async Task<ActionResult<IEnumerable<PendingFreezeUseResponse>>> GetPendingFreezeUses(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var pending = await _streakService.GetPendingFreezeUsesAsync(userId, cancellationToken);
                return Ok(pending.Select(PendingFreezeUseResponse.FromResult));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("freezes-usados-pendentes/confirmar")]
        public async Task<ActionResult<AcknowledgeFreezeUsesResponse>> AcknowledgeFreezeUses(
            [FromBody] AcknowledgeFreezeUsesRequest? request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var acknowledged = await _streakService.AcknowledgeFreezeUsesAsync(userId, request?.Ids, cancellationToken);
            return Ok(new AcknowledgeFreezeUsesResponse(acknowledged));
        }

        // O userId vem SEMPRE do token (claim NameIdentifier), nunca do corpo da requisição.
        private bool TryGetUserId(out Guid userId) {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out userId);
        }
    }
}
