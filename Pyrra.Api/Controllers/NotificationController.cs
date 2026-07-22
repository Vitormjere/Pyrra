using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pyrra.Api.Dtos.Notificacoes;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Notificacoes;

namespace Pyrra.Api.Controllers {
    [ApiController]
    [Authorize]
    [Route("api/notificacoes")]
    public class NotificationController : ControllerBase {
        private readonly INightlyMessageService _nightlyMessageService;

        public NotificationController(INightlyMessageService nightlyMessageService) {
            _nightlyMessageService = nightlyMessageService;
        }

        // Devolve o texto que SERIA enviado hoje, no estado atual do usuário, sem esperar o horário
        // configurado nem integrar push. É o ponto de teste da lógica de mensagem isolada.
        [HttpGet("preview")]
        public async Task<ActionResult<NightlyMessagePreviewResponse>> Preview(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var message = await _nightlyMessageService.GenerateClosingMessageAsync(userId, cancellationToken);
                return Ok(NightlyMessagePreviewResponse.FromMessage(message));
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
