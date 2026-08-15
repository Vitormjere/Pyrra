using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pyrra.Api.Dtos.Auth;
using Pyrra.Application.Auth;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;

namespace Pyrra.Api.Controllers {
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;

        public AuthController(IAuthService authService, IUserRepository userRepository) {
            _authService    = authService;
            _userRepository = userRepository;
        }

        [EnableRateLimiting("AuthRegister")]
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken) {
            try {
                var result = await _authService.RegisterAsync(request.Email, request.Password, request.Name, cancellationToken);
                return Ok(ToResponse(result));
            } catch (EmailAlreadyRegisteredException ex) {
                return Conflict(new { message = ex.Message });
            } catch (WeakPasswordException ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        [EnableRateLimiting("AuthLogin")]
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken) {
            try {
                var result = await _authService.LoginAsync(request.Email, request.Password, cancellationToken);
                return Ok(ToResponse(result));
            } catch (InvalidCredentialsException ex) {
                return Unauthorized(new { message = ex.Message });
            } catch (AccountLockedException ex) {
                // 423 (não 401/429) pra não colidir com o mapeamento de 401 do frontend nem
                // se misturar com o rate limit por IP — é um bloqueio de conta, coisa diferente
                Response.Headers.RetryAfter = ex.RetryAfterSeconds.ToString();
                return StatusCode(StatusCodes.Status423Locked, new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken) {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim is null || !System.Guid.TryParse(userIdClaim, out var userId)) {
                return Unauthorized();
            }

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null) {
                return NotFound();
            }

            return Ok(UserResponse.FromEntity(user));
        }

        private static AuthResponse ToResponse(AuthResult result) =>
            new(result.UserId, result.Email, result.Name, result.Token, result.ExpiresAt);
    }
}
