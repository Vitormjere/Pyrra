using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Focos {
    public record CreateFocusRequest(
        [Required][MaxLength(100)] string Name);
}
