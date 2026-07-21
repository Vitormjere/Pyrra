using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pyrra.Api.Dtos.Nutricao;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Nutricao;

namespace Pyrra.Api.Controllers {
    [ApiController]
    [Authorize]
    [Route("api/nutricao")]
    public class NutritionController : ControllerBase {
        private readonly INutritionService _nutritionService;

        public NutritionController(INutritionService nutritionService) {
            _nutritionService = nutritionService;
        }

        [HttpPost]
        public async Task<ActionResult<NutritionItemResponse>> AddItem(AddNutritionItemRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var entry = await _nutritionService.AddItemAsync(
                    userId,
                    request.MealType!.Value,
                    request.ItemName,
                    request.Quantity,
                    request.Date,
                    cancellationToken);

                return Created($"/api/nutricao/{entry.Id}", NutritionItemResponse.FromEntity(entry));
            } catch (InvalidNutritionEntryException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<DayNutritionResponse>> GetForDay([FromQuery] DateOnly? date, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var day = await _nutritionService.GetForDayAsync(userId, date, cancellationToken);
                return Ok(DayNutritionResponse.FromDay(day));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("semana")]
        public async Task<ActionResult<WeekNutritionResponse>> GetForWeek([FromQuery(Name = "inicio")] DateOnly? inicio, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var week = await _nutritionService.GetForWeekAsync(userId, inicio, cancellationToken);
                return Ok(WeekNutritionResponse.FromWeek(week));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> RemoveItem(Guid id, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                await _nutritionService.RemoveItemAsync(userId, id, cancellationToken);
                return NoContent();
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // O userId vem SEMPRE do token (claim NameIdentifier), nunca do corpo da requisição,
        // impedindo que um usuário leia ou remova itens de outro.
        private bool TryGetUserId(out Guid userId) {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out userId);
        }
    }
}
