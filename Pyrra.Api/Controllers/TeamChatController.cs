using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Pyrra.Api.Dtos.Chat;
using Pyrra.Api.Hubs;
using Pyrra.Application.Chat;
using Pyrra.Application.Common.Exceptions;

namespace Pyrra.Api.Controllers {
    // chat em grupo do time — visível só pra dono e membros, nunca pra quem é de fora
    [ApiController]
    [Authorize]
    [Route("api/times/{teamId:guid}/chat")]
    public class TeamChatController : ControllerBase {
        private readonly ITeamChatService _teamChatService;
        private readonly IHubContext<ChatHub> _hubContext;

        public TeamChatController(ITeamChatService teamChatService, IHubContext<ChatHub> hubContext) {
            _teamChatService = teamChatService;
            _hubContext      = hubContext;
        }

        [HttpGet("mensagens")]
        public async Task<ActionResult<IEnumerable<TeamChatMessageResponse>>> GetMessages(Guid teamId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var messages = await _teamChatService.GetMessagesAsync(userId, teamId, cancellationToken);
                return Ok(messages.Select(TeamChatMessageResponse.FromSummary));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("mensagens")]
        public async Task<ActionResult<TeamChatMessageResponse>> SendMessage(
            Guid teamId, SendTeamChatMessageRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var result = await _teamChatService.SendMessageAsync(userId, teamId, request.Content, cancellationToken);
                var response = TeamChatMessageResponse.FromSummary(result.Message);

                // push em tempo real só pros outros membros do time atual — quem mandou já tem a mensagem na resposta REST
                if (result.RecipientUserIds.Count > 0) {
                    await _hubContext.Clients
                        .Users(result.RecipientUserIds.Select(id => id.ToString()).ToList())
                        .SendAsync("ReceiveTeamMessage", response, cancellationToken);
                }

                return Created($"/api/times/{teamId}/chat/mensagens", response);
            } catch (InvalidChatMessageException ex) {
                return BadRequest(new { message = ex.Message });
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
