using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Focos {
    public record UpdateFocusRequest(
        [Required][MaxLength(100)] string Name);
}
