using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pyrra.Api.Dtos.Achievements;
using Pyrra.Application.Achievements;

namespace Pyrra.Api.Controllers {
    [ApiController]
    [Authorize]
    [Route("api/conquistas")]
    public class AchievementController : ControllerBase {
        private readonly IAchievementService _achievementService;

        public AchievementController(IAchievementService achievementService) {
            _achievementService = achievementService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AchievementResponse>>> GetAll(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var achievements = await _achievementService.GetForUserAsync(userId, cancellationToken);
            return Ok(achievements.Select(AchievementResponse.FromResult));
        }

        [HttpGet("pendentes")]
        public async Task<ActionResult<IEnumerable<PendingAchievementResponse>>> GetPending(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var pending = await _achievementService.GetPendingUnlocksAsync(userId, cancellationToken);
            return Ok(pending.Select(PendingAchievementResponse.FromResult));
        }

        [HttpPost("pendentes/confirmar")]
        public async Task<ActionResult<AcknowledgeAchievementsResponse>> AcknowledgePending(
            [FromBody] AcknowledgeAchievementsRequest? request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var acknowledged = await _achievementService.AcknowledgeUnlocksAsync(userId, request?.Ids, cancellationToken);
            return Ok(new AcknowledgeAchievementsResponse(acknowledged));
        }

        private bool TryGetUserId(out Guid userId) {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out userId);
        }
    }
}
