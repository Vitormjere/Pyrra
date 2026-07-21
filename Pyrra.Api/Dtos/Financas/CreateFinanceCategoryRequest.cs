using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Financas {
    public record CreateFinanceCategoryRequest(
        [Required][MaxLength(100)] string Name);
}
