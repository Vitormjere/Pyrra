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
