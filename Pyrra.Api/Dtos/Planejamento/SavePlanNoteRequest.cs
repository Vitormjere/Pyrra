using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Planejamento {
    // [Required] barra o campo ausente/null, mas string vazia passa de propósito:
    // limpar a nota do dia é uma edição válida.
    public record SavePlanNoteRequest(
        [Required(AllowEmptyStrings = true)] string Content);
}
