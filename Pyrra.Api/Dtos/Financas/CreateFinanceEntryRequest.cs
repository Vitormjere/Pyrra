using System;
using System.ComponentModel.DataAnnotations;
using Pyrra.Domain.Financas;

namespace Pyrra.Api.Dtos.Financas {
    public record CreateFinanceEntryRequest(
        [Required] Guid? CategoryId,
        [Required] decimal? Amount,
        [Required] FinanceEntryType? Type,
        DateOnly? Date = null,
        [MaxLength(500)] string? Description = null);
}
