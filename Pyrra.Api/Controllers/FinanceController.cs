using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pyrra.Api.Dtos.Financas;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Financas;

namespace Pyrra.Api.Controllers {
    [ApiController]
    [Authorize]
    [Route("api/financas")]
    public class FinanceController : ControllerBase {
        private readonly IFinanceService _financeService;

        public FinanceController(IFinanceService financeService) {
            _financeService = financeService;
        }

        [HttpGet("categorias")]
        public async Task<ActionResult<IEnumerable<FinanceCategoryResponse>>> GetCategories(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            var categories = await _financeService.GetCategoriesAsync(userId, cancellationToken);
            return Ok(categories.Select(FinanceCategoryResponse.FromEntity));
        }

        [HttpPost("categorias")]
        public async Task<ActionResult<FinanceCategoryResponse>> CreateCategory(CreateFinanceCategoryRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var category = await _financeService.CreateCategoryAsync(userId, request.Name, cancellationToken);
                return Created($"/api/financas/categorias/{category.Id}", FinanceCategoryResponse.FromEntity(category));
            } catch (DuplicateFinanceCategoryException ex) {
                return Conflict(new { message = ex.Message });
            } catch (InvalidFinanceEntryException ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("lancamentos")]
        public async Task<ActionResult<FinanceEntryResponse>> CreateEntry(CreateFinanceEntryRequest request, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var entry = await _financeService.CreateEntryAsync(
                    userId,
                    request.CategoryId!.Value,
                    request.Amount!.Value,
                    request.Type!.Value,
                    request.Date,
                    request.Description,
                    cancellationToken);

                return Created($"/api/financas/lancamentos/{entry.Id}", FinanceEntryResponse.FromEntity(entry));
            } catch (InvalidFinanceEntryException ex) {
                return BadRequest(new { message = ex.Message });
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("saldo")]
        public async Task<ActionResult<BalanceResponse>> GetBalance(CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var totals = await _financeService.GetBalanceAsync(userId, cancellationToken);
                return Ok(BalanceResponse.FromTotals(totals));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("semana")]
        public async Task<ActionResult<WeeklyFinanceSummaryResponse>> GetWeeklySummary([FromQuery(Name = "inicio")] DateOnly? inicio, CancellationToken cancellationToken) {
            if (!TryGetUserId(out var userId)) {
                return Unauthorized();
            }

            try {
                var summary = await _financeService.GetWeeklySummaryAsync(userId, inicio, cancellationToken);
                return Ok(WeeklyFinanceSummaryResponse.FromSummary(summary));
            } catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // O userId vem SEMPRE do token (claim NameIdentifier), nunca do corpo da requisição.
        // É o que garante que a listagem de categorias e o extrato nunca cruzem de usuário.
        private bool TryGetUserId(out Guid userId) {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out userId);
        }
    }
}
