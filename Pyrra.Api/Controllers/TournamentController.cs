using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pyrra.Api.Dtos.Comunidade;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Comunidade;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Api.Controllers {
    [ApiController]
    [Authorize]
    [Route("api/torneios")]
    public class TournamentController : ControllerBase {
        private readonly ITournamentService _tournamentService;

        public TournamentController(ITournamentService tournamentService) {
            _tournamentService = tournamentService;
        }

        // cria o torneio direto (admin)
        [HttpPost]
        public async Task<ActionResult<TournamentSummaryResponse>> Create(CreateTournamentRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var bannerTheme = ParseBannerTheme(request.BannerTheme);

            try {
                var tournament = await _tournamentService.CreateOfficialAsync(userId, request.Name, request.Description, bannerTheme, cancellationToken);
                return Ok(TournamentSummaryResponse.FromSummary(tournament));
            } catch (ForbiddenException ex) {
                return StatusCode(403, new { message = ex.Message });
            } catch (InvalidTournamentException ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        // solicita a criação de um torneio
        [HttpPost("solicitacoes")]
        public async Task<ActionResult<TournamentRequestResponse>> RequestTournament(RequestTournamentRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var result = await _tournamentService.RequestTournamentAsync(userId, request.Name, request.Description, cancellationToken);
                return Created($"/api/torneios/solicitacoes/{result.Id}", TournamentRequestResponse.FromSummary(result));
            } catch (InvalidTournamentException ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        // solicitações pendentes (admin)
        [HttpGet("solicitacoes")]
        public async Task<ActionResult<IEnumerable<TournamentRequestResponse>>> GetPendingRequests(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var pending = await _tournamentService.GetPendingRequestsAsync(userId, cancellationToken);
                return Ok(pending.Select(TournamentRequestResponse.FromSummary));
            } catch (ForbiddenException ex) {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        // todas as solicitações, qualquer status — pendentes + histórico (admin)
        [HttpGet("solicitacoes/todas")]
        public async Task<ActionResult<IEnumerable<TournamentRequestResponse>>> GetAllRequests(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var all = await _tournamentService.GetAllRequestsAsync(userId, cancellationToken);
                return Ok(all.Select(TournamentRequestResponse.FromSummary));
            } catch (ForbiddenException ex) {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        // aprova a solicitação (dono pede, admin deixa)
        [HttpPost("solicitacoes/{requestId:guid}/aprovar")]
        public async Task<ActionResult<TournamentSummaryResponse>> ApproveRequest(Guid requestId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var tournament = await _tournamentService.ApproveRequestAsync(userId, requestId, cancellationToken);
                return Ok(TournamentSummaryResponse.FromSummary(tournament));
            } catch (ForbiddenException ex) {
                return StatusCode(403, new { message = ex.Message });
            } catch (InvalidTournamentException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // recusa a solicitação (admin)
        [HttpPost("solicitacoes/{requestId:guid}/recusar")]
        public async Task<IActionResult> RejectRequest(Guid requestId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _tournamentService.RejectRequestAsync(userId, requestId, cancellationToken);
                return NoContent();
            } catch (ForbiddenException ex) {
                return StatusCode(403, new { message = ex.Message });
            } catch (InvalidTournamentException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // todos os torneios existentes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TournamentSummaryResponse>>> GetAll(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var tournaments = await _tournamentService.GetAllAsync(userId, cancellationToken);
            return Ok(tournaments.Select(TournamentSummaryResponse.FromSummary));
        }

        // torneios cujo dono sou eu
        [HttpGet("meus")]
        public async Task<ActionResult<IEnumerable<TournamentSummaryResponse>>> GetMyTournaments(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var tournaments = await _tournamentService.GetMyTournamentsAsync(userId, cancellationToken);
            return Ok(tournaments.Select(TournamentSummaryResponse.FromSummary));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TournamentDetailsResponse>> GetDetails(Guid id, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var details = await _tournamentService.GetDetailsAsync(userId, id, cancellationToken);
                return Ok(TournamentDetailsResponse.FromDetails(details));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{id:guid}/tema")]
        public async Task<ActionResult<TournamentSummaryResponse>> SetBannerTheme(Guid id, SetTournamentBannerThemeRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            if (!Enum.TryParse<TeamBannerTheme>(request.BannerTheme, out var theme)) {
                return BadRequest(new { message = "Cor de banner inválida." });
            }

            try {
                var tournament = await _tournamentService.SetBannerThemeAsync(userId, id, theme, cancellationToken);
                return Ok(TournamentSummaryResponse.FromSummary(tournament));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{id:guid}/banner")]
        [RequestSizeLimit(3 * 1024 * 1024 + 1024)]
        public async Task<ActionResult<TournamentSummaryResponse>> SetBannerImage(Guid id, IFormFile file, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            if (file is null || file.Length == 0) {
                return BadRequest(new { message = "Envie um arquivo de imagem." });
            }

            try {
                await using var stream = file.OpenReadStream();
                var tournament = await _tournamentService.SetBannerImageAsync(userId, id, stream, file.ContentType, file.Length, cancellationToken);
                return Ok(TournamentSummaryResponse.FromSummary(tournament));
            } catch (InvalidTournamentException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // remove a imagem customizada e volta a exibir a cor do tema
        [HttpDelete("{id:guid}/banner")]
        public async Task<ActionResult<TournamentSummaryResponse>> RemoveBannerImage(Guid id, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var tournament = await _tournamentService.RemoveBannerImageAsync(userId, id, cancellationToken);
                return Ok(TournamentSummaryResponse.FromSummary(tournament));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // só o dono do time pode solicitar entrada no torneio
        [HttpPost("{id:guid}/times/{teamId:guid}")]
        public async Task<ActionResult<TournamentTeamResponse>> RequestTeamEntry(Guid id, Guid teamId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var entry = await _tournamentService.RequestTeamEntryAsync(userId, teamId, id, cancellationToken);
                return Created($"/api/torneios/{id}/entradas", TournamentTeamResponse.FromSummary(entry));
            } catch (InvalidTournamentException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // mesma coisa, resolvendo o torneio pelo token de convite
        [HttpPost("convite/{inviteToken}/times/{teamId:guid}")]
        public async Task<ActionResult<TournamentTeamResponse>> RequestTeamEntryViaInvite(string inviteToken, Guid teamId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var entry = await _tournamentService.RequestTeamEntryViaInviteAsync(userId, teamId, inviteToken, cancellationToken);
                return Created($"/api/torneios/{entry.TournamentId}/entradas", TournamentTeamResponse.FromSummary(entry));
            } catch (InvalidTournamentException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // fila de entradas pendentes do torneio, só o dono vê
        [HttpGet("{id:guid}/entradas")]
        public async Task<ActionResult<IEnumerable<TournamentTeamResponse>>> GetPendingEntries(Guid id, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var pending = await _tournamentService.GetPendingEntriesAsync(userId, id, cancellationToken);
                return Ok(pending.Select(TournamentTeamResponse.FromSummary));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // aprova a entrada do time, só o dono do torneio
        [HttpPost("{id:guid}/entradas/{tournamentTeamId:guid}/aprovar")]
        public async Task<IActionResult> ApproveEntry(Guid id, Guid tournamentTeamId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _tournamentService.ApproveEntryAsync(userId, id, tournamentTeamId, cancellationToken);
                return NoContent();
            } catch (InvalidTournamentException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // recusa a entrada do time, só o dono do torneio
        [HttpPost("{id:guid}/entradas/{tournamentTeamId:guid}/recusar")]
        public async Task<IActionResult> RejectEntry(Guid id, Guid tournamentTeamId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _tournamentService.RejectEntryAsync(userId, id, tournamentTeamId, cancellationToken);
                return NoContent();
            } catch (InvalidTournamentException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("{id:guid}/ranking")]
        public async Task<ActionResult<IEnumerable<TournamentTeamResponse>>> GetRanking(Guid id, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var ranking = await _tournamentService.GetRankingAsync(id, cancellationToken);
                return Ok(ranking.Select(TournamentTeamResponse.FromSummary));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        private bool TryGetUserId(out Guid userId) {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out userId);
        }

        private static TeamBannerTheme ParseBannerTheme(string? value) =>
            Enum.TryParse<TeamBannerTheme>(value, out var theme) ? theme : TeamBannerTheme.Verde;
    }
}
