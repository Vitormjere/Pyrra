using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pyrra.Api.Dtos.Planejamento;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Planejamento;

namespace Pyrra.Api.Controllers {
    [ApiController]
    [Authorize]
    [Route("api/planejamento")]
    public class PlanNoteController : ControllerBase {
        private readonly IDailyPlanNoteService _planNoteService;

        public PlanNoteController(IDailyPlanNoteService planNoteService) {
            _planNoteService = planNoteService;
        }

        // PUT e não POST: a operação é idempotente por (usuário, data) — salvar duas vezes o mesmo
        // conteúdo no mesmo dia deixa o sistema no mesmo estado, e não existe "criar uma segunda
        // nota". Por isso responde 200, nunca 201.
        [HttpPut]
        public async Task<ActionResult<PlanNoteResponse>> Save([FromQuery] DateOnly? date, SavePlanNoteRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var note = await _planNoteService.SaveAsync(userId, date, request.Content, cancellationToken);
                return Ok(PlanNoteResponse.FromEntity(note));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<PlanNoteResponse>> Get([FromQuery] DateOnly? date, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var result = await _planNoteService.GetByDateAsync(userId, date, cancellationToken);
                return Ok(result.Note is null
                    ? PlanNoteResponse.Empty(result.Date)
                    : PlanNoteResponse.FromEntity(result.Note));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Histórico de reflexões: só os dias em que o usuário escreveu algo.
        [HttpGet("historico")]
        public async Task<ActionResult<IEnumerable<PlanNoteResponse>>> GetHistory([FromQuery(Name = "dias")] int dias = 30, CancellationToken cancellationToken = default) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var notes = await _planNoteService.GetHistoryAsync(userId, dias, cancellationToken);
                return Ok(notes.Select(PlanNoteResponse.FromEntity));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // O userId vem SEMPRE do token (claim NameIdentifier), nunca do corpo da requisição,
        // impedindo que um usuário leia ou sobrescreva a nota de outro.
        private bool TryGetUserId(out Guid userId) {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out userId);
        }
    }
}
