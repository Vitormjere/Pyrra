using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Nutricao {
    public record UpdateNutritionItemRequest(
        [Required][MaxLength(200)] string ItemName,
        [Required][MaxLength(100)] string Quantity);
}
