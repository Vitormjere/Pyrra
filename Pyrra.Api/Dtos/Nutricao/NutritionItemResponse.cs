using System;
using Pyrra.Domain.Nutricao;

namespace Pyrra.Api.Dtos.Nutricao {
    // MealType não se repete aqui: o item já vive dentro do grupo da sua refeição.
    public record NutritionItemResponse(
        Guid     Id,
        string   ItemName,
        string   Quantity,
        DateTime CreatedAt) {
        public static NutritionItemResponse FromEntity(NutritionEntry entry) =>
            new(entry.Id, entry.ItemName, entry.Quantity, entry.CreatedAt);
    }
}
