using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pyrra.Api.Dtos.Auth;
using Pyrra.Api.Dtos.Usuario;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Usuario;

namespace Pyrra.Api.Controllers {
    [ApiController]
    [Authorize]
    [Route("api/usuario")]
    public class UserController : ControllerBase {
        private readonly IUserPreferencesService _preferencesService;

        public UserController(IUserPreferencesService preferencesService) {
            _preferencesService = preferencesService;
        }

        [HttpPatch("preferencias")]
        public async Task<ActionResult<UserResponse>> UpdatePreferences(UpdatePreferencesRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var user = await _preferencesService.UpdatePreferencesAsync(
                    userId,
                    request.CommunicationTone!.Value,
                    request.EveningNotificationTime!.Value,
                    cancellationToken);

                // Devolve o usuário atualizado pela mesma projeção do /auth/me — sem senha.
                return Ok(UserResponse.FromEntity(user));
            } catch (InvalidPreferencesException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // O userId vem SEMPRE do token (claim NameIdentifier), nunca do corpo da requisição.
        private bool TryGetUserId(out Guid userId) {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out userId);
        }
    }
}
