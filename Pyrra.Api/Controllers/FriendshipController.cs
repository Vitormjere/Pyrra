using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pyrra.Api.Dtos.Comunidade;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Comunidade;

namespace Pyrra.Api.Controllers {
    [ApiController]
    [Authorize]
    [Route("api/amigos")]
    public class FriendshipController : ControllerBase {
        private readonly IFriendshipService _friendshipService;
        private readonly IRankingService    _rankingService;

        public FriendshipController(IFriendshipService friendshipService, IRankingService rankingService) {
            _friendshipService = friendshipService;
            _rankingService    = rankingService;
        }

        // Busca por username ou email
        [HttpGet("buscar")]
        public async Task<ActionResult<IEnumerable<UserSearchResultResponse>>> Search([FromQuery(Name = "termo")] string termo, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var results = await _friendshipService.SearchUsersAsync(userId, termo ?? string.Empty, cancellationToken);
            return Ok(results.Select(UserSearchResultResponse.FromResult));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FriendResponse>>> GetFriends(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var friends = await _friendshipService.GetFriendsAsync(userId, cancellationToken);
            return Ok(friends.Select(FriendResponse.FromSummary));
        }

        // Retorna apenas a quantidade de amigos para o perfil, sem carregar a lista completa
        [HttpGet("contagem")]
        public async Task<ActionResult<FriendCountResponse>> GetFriendsCount(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var count = await _friendshipService.GetFriendsCountAsync(userId, cancellationToken);
            return Ok(new FriendCountResponse(count));
        }

        // Retorna o ranking do usuário e de seus amigos com base na sequência atual
        [HttpGet("ranking")]
        public async Task<ActionResult<IEnumerable<RankingEntryResponse>>> GetRanking(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var ranking = await _rankingService.GetRankingAsync(userId, cancellationToken);
                return Ok(ranking.Select((e, index) => RankingEntryResponse.FromEntry(e, index + 1)));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("pedidos")]
        public async Task<ActionResult<IEnumerable<FriendRequestResponse>>> GetPendingRequests(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var pending = await _friendshipService.GetPendingReceivedAsync(userId, cancellationToken);
            return Ok(pending.Select(FriendRequestResponse.FromSummary));
        }

        // Retorna apenas a quantidade para o badge do menu, sem carregar a lista completa
        [HttpGet("pedidos/contagem")]
        public async Task<ActionResult<PendingCountResponse>> GetPendingCount(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var count = await _friendshipService.GetPendingReceivedCountAsync(userId, cancellationToken);
            return Ok(new PendingCountResponse(count));
        }

        [HttpPost("pedidos")]
        public async Task<IActionResult> SendRequest(SendFriendRequestRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _friendshipService.SendRequestAsync(userId, request.AddresseeId!.Value, cancellationToken);
                return NoContent();
            } catch (InvalidFriendshipException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("pedidos/{id:guid}/aceitar")]
        public async Task<IActionResult> Accept(Guid id, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _friendshipService.AcceptAsync(userId, id, cancellationToken);
                return NoContent();
            } catch (InvalidFriendshipException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("pedidos/{id:guid}/recusar")]
        public async Task<IActionResult> Decline(Guid id, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _friendshipService.DeclineAsync(userId, id, cancellationToken);
                return NoContent();
            } catch (InvalidFriendshipException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Desfaz uma amizade ou cancela um pedido enviado
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _friendshipService.RemoveAsync(userId, id, cancellationToken);
                return NoContent();
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // retorna o link de convite do usuário, criando automaticamente caso ainda não exista
        [HttpGet("convite")]
        public async Task<ActionResult<InviteLinkResponse>> GetInviteLink(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var token = await _friendshipService.GetOrCreateInviteTokenAsync(userId, cancellationToken);
                return Ok(InviteLinkResponse.FromToken(token));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // processa um convite por link; pedidos duplicados ou amizades existentes não são tratados como erro
        [HttpPost("convite/{token}/aceitar")]
        public async Task<ActionResult<InviteResultResponse>> AcceptInvite(string token, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var result = await _friendshipService.AcceptInviteAsync(userId, token, cancellationToken);
                return Ok(InviteResultResponse.FromResult(result));
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
