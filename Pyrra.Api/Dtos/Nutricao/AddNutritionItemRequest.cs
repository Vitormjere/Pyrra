using System;
using System.ComponentModel.DataAnnotations;
using Pyrra.Domain.Nutricao;

namespace Pyrra.Api.Dtos.Nutricao {
    public record AddNutritionItemRequest(
        [Required] MealType? MealType,
        [Required][MaxLength(200)] string ItemName,
        [Required][MaxLength(100)] string Quantity,
        DateOnly? Date = null);
}
