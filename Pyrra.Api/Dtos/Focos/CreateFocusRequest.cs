using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Focos {
    public record CreateFocusRequest(
        [Required] string Name);
}
