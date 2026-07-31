using System;
using System.Security.Claims;
using System.Text.RegularExpressions;
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
        private readonly IUsernameService _usernameService;
        private readonly IUserAccountService _accountService;
        private readonly IUserProfileService _profileService;

        public UserController(
            IUserPreferencesService preferencesService,
            IUsernameService        usernameService,
            IUserAccountService     accountService,
            IUserProfileService     profileService) {
            _preferencesService = preferencesService;
            _usernameService    = usernameService;
            _accountService     = accountService;
            _profileService     = profileService;
        }

        // Métodos de edição da conta, seguindo a ordem da tela de config

        [HttpPatch("nome")]
        public async Task<ActionResult<UserResponse>> UpdateName(UpdateNameRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var user = await _accountService.UpdateNameAsync(userId, request.Name, cancellationToken);
                return Ok(UserResponse.FromEntity(user));
            } catch (InvalidAccountException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPatch("email")]
        public async Task<ActionResult<UserResponse>> ChangeEmail(ChangeEmailRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var user = await _accountService.ChangeEmailAsync(userId, request.NewEmail, request.CurrentPassword, cancellationToken);
                return Ok(UserResponse.FromEntity(user));
            } catch (InvalidAccountException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (IncorrectPasswordException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (EmailAlreadyRegisteredException ex) {
                return Conflict(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPatch("senha")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _accountService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, cancellationToken);
                return NoContent();
            } catch (WeakPasswordException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (IncorrectPasswordException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPatch("fuso")]
        public async Task<ActionResult<UserResponse>> UpdateTimezone(UpdateTimezoneRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var user = await _accountService.UpdateTimezoneAsync(userId, request.Timezone, cancellationToken);
                return Ok(UserResponse.FromEntity(user));
            } catch (InvalidAccountException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPatch("privacidade")]
        public async Task<ActionResult<UserResponse>> UpdateProfileVisibility(UpdateProfileVisibilityRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var user = await _accountService.UpdateProfileVisibilityAsync(userId, request.Visibility!.Value, cancellationToken);
            return Ok(UserResponse.FromEntity(user));
        }

        // Perfil PÚBLICO de terceiro, por username — a única rota deste controller em que o alvo
        // vem da URL, não do token. O token ainda identifica QUEM está pedindo (viewerId), usado
        // para a regra de visibilidade: dono sempre vê; SomenteAmigos exige amizade confirmada.
        [HttpGet("{username}/perfil")]
        public async Task<ActionResult<PublicProfileResponse>> GetPublicProfile(string username, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var viewerId)) {
                return Unauthorized();
            }

            try {
                var profile = await _profileService.GetPublicProfileAsync(viewerId, username, cancellationToken);
                return Ok(PublicProfileResponse.FromResult(profile));
            } catch (PrivateProfileException ex) {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Marca o usuário como excluído, bloqueando o acesso nas próximas requisições
        [HttpDelete]
        public async Task<IActionResult> DeleteAccount(DeleteAccountRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _accountService.DeleteAccountAsync(userId, request.CurrentPassword, cancellationToken);
                return NoContent();
            } catch (IncorrectPasswordException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Define ou altera o username e retorna o usuário atualizado
        [HttpPut("username")]
        public async Task<ActionResult<UserResponse>> SetUsername(SetUsernameRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var user = await _usernameService.SetUsernameAsync(userId, request.Username, cancellationToken);
                return Ok(UserResponse.FromEntity(user));
            } catch (InvalidUsernameException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (UsernameAlreadyTakenException ex) {
                return Conflict(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Verifica a disponibilidade do username sem salvar nenhuma alteração
        [HttpGet("username/disponivel")]
        public async Task<ActionResult<UsernameAvailabilityResponse>> CheckUsername([FromQuery] string username, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var result = await _usernameService.CheckAvailabilityAsync(userId, username ?? string.Empty, cancellationToken);
            return Ok(new UsernameAvailabilityResponse(result.Available, result.Reason));
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

        [HttpPost("onboarding/concluir")]
        public async Task<ActionResult<UserResponse>> CompleteOnboarding(CompleteOnboardingRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var user = await _preferencesService.CompleteOnboardingAsync(
                    userId,
                    request.CommunicationTone,
                    request.EveningNotificationTime,
                    cancellationToken);

                return Ok(UserResponse.FromEntity(user));
            } catch (InvalidPreferencesException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        private bool TryGetUserId(out Guid userId) {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out userId);
        }
    }
}