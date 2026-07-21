using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pyrra.Api.Dtos.Tarefas;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Tarefas;

namespace Pyrra.Api.Controllers {
    [ApiController]
    [Authorize]
    [Route("api/tarefas")]
    public class PriorityTaskController : ControllerBase {
        private readonly IPriorityTaskService _taskService;

        public PriorityTaskController(IPriorityTaskService taskService) {
            _taskService = taskService;
        }

        [HttpPost]
        public async Task<ActionResult<TaskResponse>> Create(CreateTaskRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var task = await _taskService.CreateAsync(userId, request.Title, request.Priority!.Value, request.Date, cancellationToken);
                return Created($"/api/tarefas/{task.Id}", TaskResponse.FromEntity(task));
            } catch (InvalidTaskException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskResponse>>> GetForDay([FromQuery] DateOnly? date, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var tasks = await _taskService.GetForDayAsync(userId, date, cancellationToken);
                return Ok(tasks.Select(TaskResponse.FromEntity));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("semana")]
        public async Task<ActionResult<WeeklyTasksResponse>> GetPendingForWeek([FromQuery(Name = "inicio")] DateOnly? inicio, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var result = await _taskService.GetPendingForWeekAsync(userId, inicio, cancellationToken);
                return Ok(WeeklyTasksResponse.FromResult(result));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPatch("{id:guid}/concluir")]
        public async Task<ActionResult<TaskResponse>> ToggleCompleted(Guid id, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var task = await _taskService.ToggleCompletedAsync(userId, id, cancellationToken);
                return Ok(TaskResponse.FromEntity(task));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // O userId vem SEMPRE do token (claim NameIdentifier), nunca do corpo da requisição,
        // impedindo que um usuário leia ou conclua tarefas de outro.
        private bool TryGetUserId(out Guid userId) {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out userId);
        }
    }
}
