using System;
using Pyrra.Domain.Financas;

namespace Pyrra.Api.Dtos.Financas {
    public record FinanceEntryResponse(
        Guid     Id,
        Guid     CategoryId,
        decimal  Amount,
        string   Type,
        DateOnly Date,
        string?  Description,
        DateTime CreatedAt) {
        // Retorna o tipo em formato de texto
        public static FinanceEntryResponse FromEntity(FinanceEntry entry) =>
            new(entry.Id, entry.CategoryId, entry.Amount, entry.Type.ToString(), entry.Date, entry.Description, entry.CreatedAt);
    }
}
