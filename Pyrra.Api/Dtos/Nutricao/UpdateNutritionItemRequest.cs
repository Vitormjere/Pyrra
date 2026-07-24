using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Nutricao {
    // Edição só de nome e quantidade — refeição e data permanecem as do registro.
    public record UpdateNutritionItemRequest(
        [Required][MaxLength(200)] string ItemName,
        [Required][MaxLength(100)] string Quantity);
}
