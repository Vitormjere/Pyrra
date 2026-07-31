using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Planejamento {
    // O texto da nota pode ser vazio
    public record SavePlanNoteRequest(
        [Required(AllowEmptyStrings = true)] string Content);
}
