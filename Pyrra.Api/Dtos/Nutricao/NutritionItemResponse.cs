using System;
using Pyrra.Domain.Nutricao;

namespace Pyrra.Api.Dtos.Nutricao {
    // A refeição não é retornada, pois já é definida pelo grupo correspondente
    public record NutritionItemResponse(
        Guid     Id,
        string   ItemName,
        string   Quantity,
        DateTime CreatedAt) {
        public static NutritionItemResponse FromEntity(NutritionEntry entry) =>
            new(entry.Id, entry.ItemName, entry.Quantity, entry.CreatedAt);
    }
}
