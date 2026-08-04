using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pyrra.Api.Dtos.Chat;
using Pyrra.Api.Dtos.Comunidade;
using Pyrra.Application.Chat;
using Pyrra.Application.Common.Exceptions;

namespace Pyrra.Api.Controllers {
    // Chat entre admin e jogadores (Fase Admin-4a) — sem gate de admin aqui: o mesmo controller
    // atende os dois lados, simétrico. O que restringe quem fala com quem é a checagem de par
    // (um admin, um jogador) dentro de SendMessage, não [Authorize(Roles=...)].
    [ApiController]
    [Authorize]
    [Route("api/chat")]
    public class ChatController : ControllerBase {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService) {
            _chatService = chatService;
        }

        // Admins ativos — o jogador usa isso pra saber com quem pode começar uma conversa.
        [HttpGet("admins")]
        public async Task<ActionResult<IEnumerable<UserSummaryResponse>>> GetActiveAdmins(CancellationToken cancellationToken) {
            if (!TryGetUserId(out _)) {
                return Unauthorized();
            }

            var admins = await _chatService.GetActiveAdminsAsync(cancellationToken);
            return Ok(admins.Select(UserSummaryResponse.FromSummary));
        }

        [HttpGet("conversas")]
        public async Task<ActionResult<IEnumerable<ChatConversationResponse>>> GetConversations(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var conversations = await _chatService.GetConversationsAsync(userId, cancellationToken);
            return Ok(conversations.Select(ChatConversationResponse.FromSummary));
        }

        [HttpGet("conversas/{counterpartId:guid}/mensagens")]
        public async Task<ActionResult<IEnumerable<ChatMessageResponse>>> GetConversation(Guid counterpartId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var messages = await _chatService.GetConversationAsync(userId, counterpartId, cancellationToken);
            return Ok(messages.Select(ChatMessageResponse.FromSummary));
        }

        [HttpPost("conversas/{counterpartId:guid}/mensagens")]
        public async Task<ActionResult<ChatMessageResponse>> SendMessage(Guid counterpartId, SendChatMessageRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var message = await _chatService.SendMessageAsync(userId, counterpartId, request.Content, cancellationToken);
                return Created($"/api/chat/conversas/{counterpartId}/mensagens", ChatMessageResponse.FromSummary(message));
            } catch (InvalidChatMessageException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("conversas/{counterpartId:guid}/marcar-lida")]
        public async Task<IActionResult> MarkAsRead(Guid counterpartId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            await _chatService.MarkConversationAsReadAsync(userId, counterpartId, cancellationToken);
            return NoContent();
        }

        private bool TryGetUserId(out Guid userId) {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out userId);
        }
    }
}
