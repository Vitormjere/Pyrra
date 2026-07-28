using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Nutricao {
    // Atualiza apenas o nome e a quantidade do registro
    public record UpdateNutritionItemRequest(
        [Required][MaxLength(200)] string ItemName,
        [Required][MaxLength(100)] string Quantity);
}
