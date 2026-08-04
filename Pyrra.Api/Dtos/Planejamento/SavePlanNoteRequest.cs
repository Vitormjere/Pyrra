using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Planejamento {
    // texto da nota pode vir vazio
    public record SavePlanNoteRequest(
        [Required(AllowEmptyStrings = true)] string Content);
}
