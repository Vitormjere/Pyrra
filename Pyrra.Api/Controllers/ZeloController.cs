using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pyrra.Api.Dtos.Zelo;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Zelo;

namespace Pyrra.Api.Controllers {
    [ApiController]
    [Authorize]
    [Route("api/zelo")]
    public class ZeloController : ControllerBase {
        // Teto de caracteres da pergunta: o Zelo responde sobre os próprios dados do usuário, não
        // precisa de texto longo, e o limite corta abuso de prompt antes de chegar à API.
        private const int MaxQuestionLength = 300;

        private readonly IZeloService _zeloService;

        public ZeloController(IZeloService zeloService) {
            _zeloService = zeloService;
        }

        [HttpPost("perguntar")]
        public async Task<ActionResult<ZeloAnswerResponse>> Ask([FromBody] ZeloQuestionRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var pergunta = request.Pergunta?.Trim();
            if (string.IsNullOrEmpty(pergunta)) {
                return BadRequest(new { message = "A pergunta não pode ficar em branco." });
            }
            if (pergunta.Length > MaxQuestionLength) {
                return BadRequest(new { message = $"A pergunta deve ter no máximo {MaxQuestionLength} caracteres." });
            }

            try {
                var resposta = await _zeloService.AskAsync(userId, pergunta, cancellationToken);
                return Ok(new ZeloAnswerResponse(resposta));
            } catch (ZeloRateLimitExceededException ex) {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // O userId vem SEMPRE do token (claim NameIdentifier), nunca do corpo da requisição.
        private bool TryGetUserId(out Guid userId) {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out userId);
        }
    }
}
