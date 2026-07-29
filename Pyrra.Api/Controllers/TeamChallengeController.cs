using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pyrra.Api.Dtos.Desafios;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Desafios;

namespace Pyrra.Api.Controllers {
    // Ponte entre Times e o catálogo de Desafios: ativação de categoria (dono) e desafios
    // disponíveis (qualquer membro). Separado do TeamController pelo mesmo motivo do
    // ChallengeCatalogController ser separado do TeamController — bounded context próprio.
    [ApiController]
    [Authorize]
    [Route("api/times/{teamId:guid}/desafios")]
    public class TeamChallengeController : ControllerBase {
        private readonly ITeamChallengeService _service;

        public TeamChallengeController(ITeamChallengeService service) {
            _service = service;
        }

        // Todas as categorias do catálogo com o flag de ativação — só o dono
        [HttpGet("categorias")]
        public async Task<ActionResult<IEnumerable<TeamCategoryStatusResponse>>> GetCategories(Guid teamId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var statuses = await _service.GetCategoriesForTeamAsync(userId, teamId, cancellationToken);
                return Ok(statuses.Select(TeamCategoryStatusResponse.FromStatus));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Ativa uma categoria para o time — só o dono, idempotente
        [HttpPost("categorias/{categoryId:guid}")]
        public async Task<IActionResult> ActivateCategory(Guid teamId, Guid categoryId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _service.ActivateCategoryAsync(userId, teamId, categoryId, cancellationToken);
                return NoContent();
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Desativa uma categoria do time — só o dono, idempotente
        [HttpDelete("categorias/{categoryId:guid}")]
        public async Task<IActionResult> DeactivateCategory(Guid teamId, Guid categoryId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _service.DeactivateCategoryAsync(userId, teamId, categoryId, cancellationToken);
                return NoContent();
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Desafios das categorias ativas do time — qualquer membro (dono ou não)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AvailableChallengeResponse>>> GetAvailableChallenges(Guid teamId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var challenges = await _service.GetAvailableChallengesAsync(userId, teamId, cancellationToken);
                return Ok(challenges.Select(AvailableChallengeResponse.FromAvailable));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Envia a prova por foto de um desafio — qualquer membro (dono ou não)
        [HttpPost("{challengeId:guid}/submissoes")]
        public async Task<ActionResult<ChallengeSubmissionResponse>> SubmitProof(Guid teamId, Guid challengeId, IFormFile file, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            if (file is null || file.Length == 0) {
                return BadRequest(new { message = "Envie um arquivo de imagem." });
            }

            try {
                await using var stream = file.OpenReadStream();
                var submission = await _service.SubmitChallengeProofAsync(
                    userId, teamId, challengeId, stream, file.ContentType, file.Length, cancellationToken);
                return Created(
                    $"/api/times/{teamId}/desafios/submissoes",
                    ChallengeSubmissionResponse.FromEntity(submission));
            } catch (InvalidChallengeException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Fila de submissões pendentes do time — só o dono
        [HttpGet("submissoes")]
        public async Task<ActionResult<IEnumerable<PendingSubmissionResponse>>> GetPendingSubmissions(Guid teamId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var pending = await _service.GetPendingSubmissionsAsync(userId, teamId, cancellationToken);
                return Ok(pending.Select(PendingSubmissionResponse.FromPending));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Aprova a submissão e soma os pontos do desafio ao time — só o dono
        [HttpPost("submissoes/{submissionId:guid}/aprovar")]
        public async Task<IActionResult> ApproveSubmission(Guid teamId, Guid submissionId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _service.ApproveSubmissionAsync(userId, teamId, submissionId, cancellationToken);
                return NoContent();
            } catch (InvalidChallengeException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Recusa a submissão, sem somar pontos — só o dono
        [HttpPost("submissoes/{submissionId:guid}/recusar")]
        public async Task<IActionResult> RejectSubmission(Guid teamId, Guid submissionId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _service.RejectSubmissionAsync(userId, teamId, submissionId, cancellationToken);
                return NoContent();
            } catch (InvalidChallengeException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Ranking de membros do time por placar INDIVIDUAL — qualquer membro (dono ou não)
        [HttpGet("ranking")]
        public async Task<ActionResult<IEnumerable<TeamMemberRankingResponse>>> GetRanking(Guid teamId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var ranking = await _service.GetTeamRankingAsync(userId, teamId, cancellationToken);
                return Ok(ranking.Select(TeamMemberRankingResponse.FromRanking));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Bytes da foto de uma submissão — container privado, só sai por aqui. Qualquer membro do
        // time (dono ou não) pode ver.
        [HttpGet("submissoes/{submissionId:guid}/foto")]
        public async Task<IActionResult> GetSubmissionPhoto(Guid teamId, Guid submissionId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var (content, contentType) = await _service.GetSubmissionPhotoAsync(userId, teamId, submissionId, cancellationToken);
                return File(content, contentType);
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
