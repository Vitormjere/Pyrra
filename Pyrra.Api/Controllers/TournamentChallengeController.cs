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

namespace Pyrra.Api.Controllers {
    // Gerencia os desafios de um torneio específico: vínculo com o catálogo geral e desafios
    // próprios — restrito ao dono do torneio (Fase 5b)
    [ApiController]
    [Authorize]
    [Route("api/torneios/{tournamentId:guid}/desafios")]
    public class TournamentChallengeController : ControllerBase {
        private readonly ITournamentChallengeService _service;

        public TournamentChallengeController(ITournamentChallengeService service) {
            _service = service;
        }

        // catálogo geral com status de vínculo ao torneio
        [HttpGet("catalogo")]
        public async Task<ActionResult<IEnumerable<TournamentCatalogChallengeResponse>>> GetCatalog(Guid tournamentId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var statuses = await _service.GetCatalogAsync(userId, tournamentId, cancellationToken);
                return Ok(statuses.Select(TournamentCatalogChallengeResponse.FromStatus));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // vincula um desafio do catálogo geral ao torneio
        [HttpPost("catalogo/{challengeId:guid}")]
        public async Task<IActionResult> LinkCatalogChallenge(Guid tournamentId, Guid challengeId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _service.LinkCatalogChallengeAsync(userId, tournamentId, challengeId, cancellationToken);
                return NoContent();
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // desvincula um desafio do catálogo geral do torneio
        [HttpDelete("catalogo/{challengeId:guid}")]
        public async Task<IActionResult> UnlinkCatalogChallenge(Guid tournamentId, Guid challengeId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _service.UnlinkCatalogChallengeAsync(userId, tournamentId, challengeId, cancellationToken);
                return NoContent();
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // desafios próprios do torneio
        [HttpGet("proprios")]
        public async Task<ActionResult<IEnumerable<TournamentOwnChallengeResponse>>> GetOwnChallenges(Guid tournamentId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var challenges = await _service.GetOwnChallengesAsync(userId, tournamentId, cancellationToken);
                return Ok(challenges.Select(TournamentOwnChallengeResponse.FromEntity));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // cria um desafio próprio do torneio
        [HttpPost("proprios")]
        public async Task<ActionResult<TournamentOwnChallengeResponse>> CreateOwnChallenge(Guid tournamentId, CreateTournamentOwnChallengeRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var challenge = await _service.CreateOwnChallengeAsync(
                    userId, tournamentId, request.Title, request.Description, request.Points!.Value, cancellationToken);
                return Created($"/api/torneios/{tournamentId}/desafios/proprios/{challenge.Id}", TournamentOwnChallengeResponse.FromEntity(challenge));
            } catch (InvalidChallengeException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // edita um desafio próprio do torneio
        [HttpPut("proprios/{challengeId:guid}")]
        public async Task<ActionResult<TournamentOwnChallengeResponse>> UpdateOwnChallenge(Guid tournamentId, Guid challengeId, UpdateTournamentOwnChallengeRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var challenge = await _service.UpdateOwnChallengeAsync(
                    userId, tournamentId, challengeId, request.Title, request.Description, request.Points!.Value, cancellationToken);
                return Ok(TournamentOwnChallengeResponse.FromEntity(challenge));
            } catch (InvalidChallengeException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // remove um desafio próprio do torneio
        [HttpDelete("proprios/{challengeId:guid}")]
        public async Task<IActionResult> DeleteOwnChallenge(Guid tournamentId, Guid challengeId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _service.DeleteOwnChallengeAsync(userId, tournamentId, challengeId, cancellationToken);
                return NoContent();
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Submissões pendentes de TODOS os times participantes do torneio — a fila do dono do
        // torneio. Aprovar/recusar continua nos endpoints de time (TeamChallengeController),
        // já que cada submissão pertence a um time específico.
        [HttpGet("submissoes")]
        public async Task<ActionResult<IEnumerable<PendingTournamentSubmissionWithTeamResponse>>> GetPendingSubmissions(Guid tournamentId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var pending = await _service.GetPendingSubmissionsAsync(userId, tournamentId, cancellationToken);
                return Ok(pending.Select(PendingTournamentSubmissionWithTeamResponse.FromPending));
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
