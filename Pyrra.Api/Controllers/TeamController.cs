using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    [Route("api/times")]
    public class TeamController : ControllerBase {
        private readonly ITeamService _teamService;

        public TeamController(ITeamService teamService) {
            _teamService = teamService;
        }

        [HttpPost]
        public async Task<ActionResult<TeamSummaryResponse>> Create(CreateTeamRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var visibility  = ParseVisibility(request.Visibility);
            var bannerTheme = ParseBannerTheme(request.BannerTheme);

            try {
                var team = await _teamService.CreateAsync(
                    userId, request.Name, request.Description, request.MemberLimit,
                    visibility, bannerTheme, cancellationToken);
                return Ok(TeamSummaryResponse.FromSummary(team));
            } catch (InvalidTeamException ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TeamSummaryResponse>>> GetMyTeams(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var teams = await _teamService.GetMyTeamsAsync(userId, cancellationToken);
            return Ok(teams.Select(TeamSummaryResponse.FromSummary));
        }

        // TODOS os times do site, públicos e privados, de qualquer dono — restrito a admins
        // (Fase Admin-2.1). Usado pela tela de Times quando IsAdmin=true, no lugar das abas
        // "Meus Times"/"Explorar" do usuário comum.
        [HttpGet("todos")]
        public async Task<ActionResult<IEnumerable<TeamSummaryResponse>>> GetAllTeams(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var teams = await _teamService.GetAllTeamsAsync(userId, cancellationToken);
                return Ok(teams.Select(TeamSummaryResponse.FromSummary));
            } catch (ForbiddenException ex) {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        // Retorna os times do usuário que podem participar de um torneio
        [HttpGet("meus/elegiveis-torneio")]
        public async Task<ActionResult<IEnumerable<TeamSummaryResponse>>> GetMyEligibleForTournament(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var teams = await _teamService.GetMyEligibleForTournamentAsync(userId, cancellationToken);
            return Ok(teams.Select(TeamSummaryResponse.FromSummary));
        }

        // Times marcados como Público - aba Explorar
        [HttpGet("publicos")]
        public async Task<ActionResult<IEnumerable<PublicTeamResponse>>> GetPublicTeams(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var teams = await _teamService.GetPublicTeamsAsync(userId, cancellationToken);
            return Ok(teams.Select(PublicTeamResponse.FromSummary));
        }

        // Altera a visibilidade do time
        [HttpPost("{id:guid}/visibilidade")]
        public async Task<IActionResult> SetVisibility(Guid id, SetTeamVisibilityRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            if (!Enum.TryParse<TeamVisibility>(request.Visibility, out var visibility)) {
                return BadRequest(new { message = "Visibilidade inválida." });
            }

            try {
                await _teamService.SetVisibilityAsync(userId, id, visibility, cancellationToken);
                return NoContent();
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Altera a cor do banner
        [HttpPost("{id:guid}/tema")]
        public async Task<ActionResult<TeamSummaryResponse>> SetBannerTheme(Guid id, SetTeamBannerThemeRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            if (!Enum.TryParse<TeamBannerTheme>(request.BannerTheme, out var theme)) {
                return BadRequest(new { message = "Cor de banner inválida." });
            }

            try {
                var team = await _teamService.SetBannerThemeAsync(userId, id, theme, cancellationToken);
                return Ok(TeamSummaryResponse.FromSummary(team));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Upload de imagem de capa 
        [HttpPost("{id:guid}/banner")]
        [RequestSizeLimit(3 * 1024 * 1024 + 1024)]
        public async Task<ActionResult<TeamSummaryResponse>> UploadBannerImage(Guid id, IFormFile file, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            if (file is null || file.Length == 0) {
                return BadRequest(new { message = "Envie um arquivo de imagem." });
            }

            try {
                await using var stream = file.OpenReadStream();
                var team = await _teamService.SetBannerImageAsync(userId, id, stream, file.ContentType, file.Length, cancellationToken);
                return Ok(TeamSummaryResponse.FromSummary(team));
            } catch (InvalidTeamException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Remove a imagem customizada e volta a exibir a cor do tema
        [HttpDelete("{id:guid}/banner")]
        public async Task<ActionResult<TeamSummaryResponse>> RemoveBannerImage(Guid id, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var team = await _teamService.RemoveBannerImageAsync(userId, id, cancellationToken);
                return Ok(TeamSummaryResponse.FromSummary(team));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TeamDetailsResponse>> GetDetails(Guid id, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var details = await _teamService.GetDetailsAsync(userId, id, cancellationToken);
                return Ok(TeamDetailsResponse.FromDetails(details));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Convite direto (só o dono pode, e se ser amigo dele)
        [HttpPost("{id:guid}/convidar")]
        public async Task<IActionResult> InviteFriend(Guid id, InviteFriendToTeamRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _teamService.InviteFriendAsync(userId, id, request.FriendUserId!.Value, cancellationToken);
                return NoContent();
            } catch (InvalidTeamException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("convites")]
        public async Task<ActionResult<IEnumerable<TeamInviteResponse>>> GetPendingInvites(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var pending = await _teamService.GetPendingReceivedInvitesAsync(userId, cancellationToken);
            return Ok(pending.Select(TeamInviteResponse.FromSummary));
        }

        // Retorna apenas a quantidade para o badge do menu, sem carregar a lista completa
        [HttpGet("convites/contagem")]
        public async Task<ActionResult<TeamInviteCountResponse>> GetPendingInvitesCount(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var count = await _teamService.GetPendingReceivedInvitesCountAsync(userId, cancellationToken);
            return Ok(new TeamInviteCountResponse(count));
        }

        [HttpPost("convites/{inviteId:guid}/aceitar")]
        public async Task<IActionResult> AcceptInvite(Guid inviteId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _teamService.AcceptInviteAsync(userId, inviteId, cancellationToken);
                return NoContent();
            } catch (InvalidTeamException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("convites/{inviteId:guid}/recusar")]
        public async Task<IActionResult> DeclineInvite(Guid inviteId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _teamService.DeclineInviteAsync(userId, inviteId, cancellationToken);
                return NoContent();
            } catch (InvalidTeamException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Processa a entrada por link e retorna o resultado da operaçao
        [HttpPost("convite/{token}/entrar")]
        public async Task<ActionResult<JoinResultResponse>> JoinViaLink(string token, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var result = await _teamService.JoinViaLinkAsync(userId, token, cancellationToken);
                return Ok(JoinResultResponse.FromResult(result));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{id:guid}/sair")]
        public async Task<IActionResult> Leave(Guid id, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _teamService.LeaveAsync(userId, id, cancellationToken);
                return NoContent();
            } catch (InvalidTeamException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Remove um membro do time
        [HttpDelete("{id:guid}/membros/{memberUserId:guid}")]
        public async Task<IActionResult> RemoveMember(Guid id, Guid memberUserId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _teamService.RemoveMemberAsync(userId, id, memberUserId, cancellationToken);
                return NoContent();
            } catch (InvalidTeamException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Exclui o time e todos os vínculos
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _teamService.DeleteTeamAsync(userId, id, cancellationToken);
                return NoContent();
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Transfere a titularidade para um membro já existente do time
        [HttpPost("{id:guid}/transferir")]
        public async Task<IActionResult> TransferOwnership(Guid id, TransferOwnershipRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _teamService.TransferOwnershipAsync(userId, id, request.NewOwnerUserId!.Value, cancellationToken);
                return NoContent();
            } catch (InvalidTeamException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        private bool TryGetUserId(out Guid userId) {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out userId);
        }

        // Quando não informado ou inválido, usa a privacidade padrão
        private static TeamVisibility ParseVisibility(string? value) =>
            Enum.TryParse<TeamVisibility>(value, out var visibility) ? visibility : TeamVisibility.Privado;

        private static TeamBannerTheme ParseBannerTheme(string? value) =>
            Enum.TryParse<TeamBannerTheme>(value, out var theme) ? theme : TeamBannerTheme.Verde;
    }
}
