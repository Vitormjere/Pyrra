using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pyrra.Api.Dtos.Desafios;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Desafios;
using Pyrra.Domain.Desafios;

namespace Pyrra.Api.Controllers {
    // Endpoints de administração do catálogo de desafios, acessíveis apenas por admins
    [ApiController]
    [Authorize]
    [Route("api/admin/desafios")]
    public class ChallengeCatalogController : ControllerBase {
        private readonly IChallengeCatalogService _catalogService;

        public ChallengeCatalogController(IChallengeCatalogService catalogService) {
            _catalogService = catalogService;
        }

        [HttpGet("categorias")]
        public async Task<ActionResult<IEnumerable<ChallengeCategoryResponse>>> GetCategories(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var categories = await _catalogService.GetCategoriesAsync(userId, cancellationToken);
                return Ok(categories.Select(ChallengeCategoryResponse.FromEntity));
            } catch (ForbiddenException ex) {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        [HttpPost("categorias")]
        public async Task<ActionResult<ChallengeCategoryResponse>> CreateCategory(CreateChallengeCategoryRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            if (!Enum.TryParse<ChallengeCategoryColor>(request.Color, out var color)) {
                return BadRequest(new { message = "Cor inválida." });
            }

            try {
                var category = await _catalogService.CreateCategoryAsync(
                    userId, request.Name, request.Description, request.Icon, color, cancellationToken);
                return Created($"/api/admin/desafios/categorias/{category.Id}", ChallengeCategoryResponse.FromEntity(category));
            } catch (ForbiddenException ex) {
                return StatusCode(403, new { message = ex.Message });
            } catch (InvalidChallengeException ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("categorias/{id:guid}")]
        public async Task<ActionResult<ChallengeCategoryResponse>> UpdateCategory(Guid id, UpdateChallengeCategoryRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            if (!Enum.TryParse<ChallengeCategoryColor>(request.Color, out var color)) {
                return BadRequest(new { message = "Cor inválida." });
            }

            try {
                var category = await _catalogService.UpdateCategoryAsync(
                    userId, id, request.Name, request.Description, request.Icon, color, cancellationToken);
                return Ok(ChallengeCategoryResponse.FromEntity(category));
            } catch (ForbiddenException ex) {
                return StatusCode(403, new { message = ex.Message });
            } catch (InvalidChallengeException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("categorias/{id:guid}")]
        public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _catalogService.DeleteCategoryAsync(userId, id, cancellationToken);
                return NoContent();
            } catch (ForbiddenException ex) {
                return StatusCode(403, new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            } catch (ChallengeCategoryInUseException ex) {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChallengeResponse>>> GetChallenges(Guid? categoriaId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var challenges = await _catalogService.GetChallengesAsync(userId, categoriaId, cancellationToken);
                return Ok(challenges.Select(ChallengeResponse.FromEntity));
            } catch (ForbiddenException ex) {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ChallengeResponse>> CreateChallenge(CreateChallengeRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var challenge = await _catalogService.CreateChallengeAsync(
                    userId, request.CategoryId!.Value, request.Title, request.Description, request.Points!.Value, request.Deadline, cancellationToken);
                return Created($"/api/admin/desafios/{challenge.Id}", ChallengeResponse.FromEntity(challenge));
            } catch (ForbiddenException ex) {
                return StatusCode(403, new { message = ex.Message });
            } catch (InvalidChallengeException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ChallengeResponse>> UpdateChallenge(Guid id, UpdateChallengeRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var challenge = await _catalogService.UpdateChallengeAsync(
                    userId, id, request.CategoryId!.Value, request.Title, request.Description, request.Points!.Value, request.Deadline, cancellationToken);
                return Ok(ChallengeResponse.FromEntity(challenge));
            } catch (ForbiddenException ex) {
                return StatusCode(403, new { message = ex.Message });
            } catch (InvalidChallengeException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteChallenge(Guid id, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _catalogService.DeleteChallengeAsync(userId, id, cancellationToken);
                return NoContent();
            } catch (ForbiddenException ex) {
                return StatusCode(403, new { message = ex.Message });
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
