using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pyrra.Api.Dtos.Treinos;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Treinos;
using Pyrra.Domain.Common;
using Pyrra.Domain.Treinos;

namespace Pyrra.Api.Controllers {
    [ApiController]
    [Authorize]
    [Route("api/treinos")]
    public class WorkoutController : ControllerBase {
        private readonly IWorkoutService _workoutService;

        public WorkoutController(IWorkoutService workoutService) {
            _workoutService = workoutService;
        }

        [HttpPost]
        public async Task<ActionResult<WorkoutResponse>> Create(CreateWorkoutRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var log = await _workoutService.CreateAsync(userId, request.ToInput(), cancellationToken);
                return Created($"/api/treinos/{log.Id}", WorkoutResponse.FromEntity(log));
            } catch (InvalidWorkoutException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (FutureDateException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WorkoutResponse>>> GetAll([FromQuery(Name = "tipo")] WorkoutType? tipo, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var logs = await _workoutService.GetAllForUserAsync(userId, tipo, cancellationToken);
            return Ok(logs.Select(WorkoutResponse.FromEntity));
        }

        // Plano semanal: sempre os 7 dias, mesmo os sem label.
        [HttpGet("plano")]
        public async Task<ActionResult<IEnumerable<WorkoutPlanDayResponse>>> GetPlan(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var plan = await _workoutService.GetPlanWithExercisesAsync(userId, cancellationToken);
            return Ok(plan.Select(WorkoutPlanDayResponse.FromEntity));
        }

        // Acrescenta um exercício ao dia. O Order é calculado no service.
        [HttpPost("plano/{diaDaSemana}/exercicios")]
        public async Task<ActionResult<WorkoutPlanExerciseResponse>> AddPlanExercise(
            WeekDay diaDaSemana,
            AddPlanExerciseRequest request,
            CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var exercise = await _workoutService.AddPlanExerciseAsync(
                    userId,
                    diaDaSemana,
                    request.Type!.Value,
                    request.ExerciseName,
                    request.Sets,
                    request.Reps,
                    cancellationToken);

                return Created(
                    $"/api/treinos/plano/exercicios/{exercise.Id}",
                    WorkoutPlanExerciseResponse.FromEntity(exercise));
            } catch (InvalidWorkoutException ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("plano/exercicios/{id:guid}")]
        public async Task<IActionResult> RemovePlanExercise(Guid id, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _workoutService.RemovePlanExerciseAsync(userId, id, cancellationToken);
                return NoContent();
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("plano")]
        public async Task<ActionResult<IEnumerable<WorkoutPlanDayResponse>>> SavePlan(SaveWorkoutPlanRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var days = request.Days
                .Select(day => new WorkoutPlanDay {
                    DayOfWeek = day.DayOfWeek!.Value,
                    Label     = day.Label
                })
                .ToList();

            await _workoutService.SavePlanAsync(userId, days, cancellationToken);

            // Devolve o plano COMPLETO, com exercícios: o PUT só altera labels, mas
            // responder num formato diferente do GET faria a tela ter dois caminhos
            // para o mesmo dado.
            var plan = await _workoutService.GetPlanWithExercisesAsync(userId, cancellationToken);
            return Ok(plan.Select(WorkoutPlanDayResponse.FromEntity));
        }

        // Reaproveita CreateWorkoutRequest: a forma do corpo é idêntica à da criação.
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<WorkoutResponse>> Update(Guid id, CreateWorkoutRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var log = await _workoutService.UpdateAsync(userId, id, request.ToInput(), cancellationToken);
                return Ok(WorkoutResponse.FromEntity(log));
            } catch (InvalidWorkoutException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (FutureDateException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _workoutService.DeleteAsync(userId, id, cancellationToken);
                return NoContent();
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // Intervalo arbitrário — usado pela Agenda. Rota própria em vez de
        // parâmetros opcionais no GET raiz: o filtro por tipo e o por período
        // respondem a perguntas diferentes e misturá-los deixaria a assinatura ambígua.
        [HttpGet("intervalo")]
        public async Task<ActionResult<IEnumerable<WorkoutResponse>>> GetForRange(
            [FromQuery(Name = "inicio")] DateOnly inicio,
            [FromQuery(Name = "fim")] DateOnly fim,
            CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var logs = await _workoutService.GetForRangeAsync(userId, inicio, fim, cancellationToken);
            return Ok(logs.Select(WorkoutResponse.FromEntity));
        }

        [HttpGet("exercicio/{nome}")]
        public async Task<ActionResult<IEnumerable<WorkoutResponse>>> GetHistoryByExercise(string nome, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var logs = await _workoutService.GetHistoryByExerciseAsync(userId, nome, cancellationToken);
                return Ok(logs.Select(WorkoutResponse.FromEntity));
            } catch (InvalidWorkoutException ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        // O userId vem SEMPRE do token (claim NameIdentifier), nunca do corpo da requisição,
        // impedindo que um usuário registre ou leia treinos de outro passando outro id no payload.
        private bool TryGetUserId(out Guid userId) {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out userId);
        }
    }
}
