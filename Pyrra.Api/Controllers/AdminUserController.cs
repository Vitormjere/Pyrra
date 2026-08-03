using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pyrra.Api.Dtos.Admin;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Usuario;

namespace Pyrra.Api.Controllers {
    // Gestão administrativa de contas (Fase Admin-2) — restrita a quem já é admin, verificado pelo
    // serviço (IAdminAuthorizationService), não por [Authorize(Roles=...)] — mesmo critério de
    // ChallengeCatalogController/TournamentController.
    [ApiController]
    [Authorize]
    [Route("api/admin")]
    public class AdminUserController : ControllerBase {
        private readonly IAdminUserService _service;

        public AdminUserController(IAdminUserService service) {
            _service = service;
        }

        // cria uma nova conta JÁ admin
        [HttpPost("contas")]
        public async Task<ActionResult<AdminUserResponse>> CreateAdminAccount(CreateAdminAccountRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var summary = await _service.CreateAdminAccountAsync(
                    userId, request.Email, request.Name, request.Username, request.Password, cancellationToken);
                return Created($"/api/admin/usuarios/{summary.Id}", AdminUserResponse.FromSummary(summary));
            } catch (ForbiddenException ex) {
                return StatusCode(403, new { message = ex.Message });
            } catch (EmailAlreadyRegisteredException ex) {
                return Conflict(new { message = ex.Message });
            } catch (UsernameAlreadyTakenException ex) {
                return Conflict(new { message = ex.Message });
            } catch (InvalidUsernameException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (WeakPasswordException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (InvalidAccountException ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        // lista todos os usuários, inclusive excluídos
        [HttpGet("usuarios")]
        public async Task<ActionResult<IEnumerable<AdminUserResponse>>> GetUsers(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var users = await _service.GetAllUsersAsync(userId, cancellationToken);
                return Ok(users.Select(AdminUserResponse.FromSummary));
            } catch (ForbiddenException ex) {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        // exclui (soft delete) a conta de um jogador, sem exigir a senha dele
        [HttpDelete("usuarios/{id:guid}")]
        public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _service.DeleteUserAsync(userId, id, cancellationToken);
                return NoContent();
            } catch (ForbiddenException ex) {
                return StatusCode(403, new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            } catch (InvalidAccountException ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        private bool TryGetUserId(out Guid userId) {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out userId);
        }
    }
}
