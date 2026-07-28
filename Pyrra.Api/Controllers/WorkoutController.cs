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

        //  os 7 dias do plano semanal, mesmo sem refeições cadastradas
        [HttpGet("plano")]
        public async Task<ActionResult<IEnumerable<WorkoutPlanDayResponse>>> GetPlan(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var plan = await _workoutService.GetPlanWithExercisesAsync(userId, cancellationToken);
            return Ok(plan.Select(WorkoutPlanDayResponse.FromEntity));
        }

        // Adiciona um exercício ao dia, definindo a ordem automaticamente.
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

            // Retorna o plano completo para manter o mesmo formato de resposta do GET
            var plan = await _workoutService.GetPlanWithExercisesAsync(userId, cancellationToken);
            return Ok(plan.Select(WorkoutPlanDayResponse.FromEntity));
        }

        // Reutiliza o CreateWorkoutRequest por ter a mesma estrutura da criação
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

        // Busca as tarefas do período informado, usado pela Agenda
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

        private bool TryGetUserId(out Guid userId) {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out userId);
        }
    }
}
