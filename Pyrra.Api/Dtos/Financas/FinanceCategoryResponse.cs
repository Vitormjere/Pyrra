using System;
using Pyrra.Domain.Financas;

namespace Pyrra.Api.Dtos.Financas {
    // UserId NÃO é exposto de propósito: o cliente não precisa saber de quem é a categoria, e
    // omitir o campo garante que nenhuma resposta carregue id de dono. IsDefault já diz o que
    // interessa — se é do sistema ou criada pelo usuário.
    public record FinanceCategoryResponse(
        Guid   Id,
        string Name,
        bool   IsDefault) {
        public static FinanceCategoryResponse FromEntity(FinanceCategory category) =>
            new(category.Id, category.Name, category.IsDefault);
    }
}
