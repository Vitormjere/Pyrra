using System;
using Pyrra.Domain.Financas;

namespace Pyrra.Api.Dtos.Financas {
    // só os dados necessários da categoria, sem expor o id do usuário
    public record FinanceCategoryResponse(
        Guid   Id,
        string Name,
        bool   IsDefault) {
        public static FinanceCategoryResponse FromEntity(FinanceCategory category) =>
            new(category.Id, category.Name, category.IsDefault);
    }
}
