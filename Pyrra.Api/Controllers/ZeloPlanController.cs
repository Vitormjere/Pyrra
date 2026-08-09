using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pyrra.Api.Dtos.Zelo;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Zelo;

namespace Pyrra.Api.Controllers {
    // Zelo conversacional: formulário guiado que monta Treino e Nutrição juntos, separado do Zelo geral (ZeloController)
    [ApiController]
    [Authorize]
    [Route("api/zelo/plano")]
    public class ZeloPlanController : ControllerBase {
        private readonly IZeloPlanService _service;

        public ZeloPlanController(IZeloPlanService service) {
            _service = service;
        }

        [HttpPost("iniciar")]
        public async Task<ActionResult<ZeloPlanSessionResponse>> Start(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var state = await _service.StartOrResumeAsync(userId, cancellationToken);
            return Ok(ZeloPlanSessionResponse.FromState(state));
        }

        [HttpPost("{sessionId:guid}/responder")]
        public async Task<ActionResult<ZeloPlanSessionResponse>> Answer(
            Guid sessionId, [FromBody] ZeloPlanAnswerRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var state = await _service.AnswerAsync(userId, sessionId, request.Resposta ?? string.Empty, cancellationToken);
                return Ok(ZeloPlanSessionResponse.FromState(state));
            } catch (ZeloPlanRateLimitExceededException ex) {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message });
            } catch (InvalidZeloPlanException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{sessionId:guid}/tentar-novamente")]
        public async Task<ActionResult<ZeloPlanSessionResponse>> RetryGeneration(Guid sessionId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var state = await _service.RetryGenerationAsync(userId, sessionId, cancellationToken);
                return Ok(ZeloPlanSessionResponse.FromState(state));
            } catch (ZeloPlanRateLimitExceededException ex) {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message });
            } catch (InvalidZeloPlanException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("{sessionId:guid}/preview")]
        public async Task<ActionResult<ZeloPlanPreviewResponse>> GetPreview(Guid sessionId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var preview = await _service.GetPreviewAsync(userId, sessionId, cancellationToken);
                return Ok(ZeloPlanPreviewResponse.FromResult(preview));
            } catch (InvalidZeloPlanException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // sobrescreve o Plano da Semana (Treino) e o plano de Nutrição do usuário com o plano gerado
        [HttpPost("{sessionId:guid}/aplicar")]
        public async Task<IActionResult> Apply(Guid sessionId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _service.ApplyAsync(userId, sessionId, cancellationToken);
                return NoContent();
            } catch (InvalidZeloPlanException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // descarta o plano gerado, mantém o que o usuário já tinha em Treino e Nutrição
        [HttpPost("{sessionId:guid}/descartar")]
        public async Task<IActionResult> Discard(Guid sessionId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _service.DiscardAsync(userId, sessionId, cancellationToken);
                return NoContent();
            } catch (InvalidZeloPlanException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // histórico do chat livre — disponível a partir de PlanoGerado
        [HttpGet("{sessionId:guid}/mensagens")]
        public async Task<ActionResult<ZeloPlanChatMessageResponse[]>> GetMessages(Guid sessionId, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var messages = await _service.GetMessagesAsync(userId, sessionId, cancellationToken);
                return Ok(messages.Select(ZeloPlanChatMessageResponse.FromResult).ToArray());
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{sessionId:guid}/mensagens")]
        public async Task<ActionResult<ZeloPlanChatResponse>> SendMessage(
            Guid sessionId, [FromBody] ZeloPlanChatRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var result = await _service.SendMessageAsync(userId, sessionId, request.Mensagem ?? string.Empty, cancellationToken);
                return Ok(ZeloPlanChatResponse.FromResult(result));
            } catch (ZeloPlanRateLimitExceededException ex) {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message });
            } catch (InvalidZeloPlanException ex) {
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
