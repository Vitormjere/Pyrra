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

        public FriendshipController(IFriendshipService friendshipService) {
            _friendshipService = friendshipService;
        }

        // Busca por username ou email; termo curto/vazio devolve lista vazia (o service trata).
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

        // Contagem para o Perfil. Rota própria, mesmo critério de pedidos/contagem: poupa hidratar
        // a lista inteira de amigos só para mostrar um número.
        [HttpGet("contagem")]
        public async Task<ActionResult<FriendCountResponse>> GetFriendsCount(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var count = await _friendshipService.GetFriendsCountAsync(userId, cancellationToken);
            return Ok(new FriendCountResponse(count));
        }

        [HttpGet("pedidos")]
        public async Task<ActionResult<IEnumerable<FriendRequestResponse>>> GetPendingRequests(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var pending = await _friendshipService.GetPendingReceivedAsync(userId, cancellationToken);
            return Ok(pending.Select(FriendRequestResponse.FromSummary));
        }

        // Contagem para o badge do menu. Rota própria para não trazer a lista inteira só para o número.
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

        // Remove/desfaz — amizade aceita (desfazer) ou pedido enviado (cancelar).
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

        // Link de convite pessoal (estável). Criado sob demanda na primeira chamada.
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

        // Abrir um convite: envia o pedido ao dono do link e devolve o desfecho. Duplicado/já-amigos
        // não são erro (o link é idempotente); token inválido é 404.
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
