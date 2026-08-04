using System;
using Pyrra.Domain.Nutricao;

namespace Pyrra.Api.Dtos.Nutricao {
    // refeição não vem aqui, já tá definida no grupo correspondente
    public record NutritionItemResponse(
        Guid     Id,
        string   ItemName,
        string   Quantity,
        DateTime CreatedAt) {
        public static NutritionItemResponse FromEntity(NutritionEntry entry) =>
            new(entry.Id, entry.ItemName, entry.Quantity, entry.CreatedAt);
    }
}
