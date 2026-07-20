using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Focos {
    public record UpdateWeightRequest(
        [Range(1, 100)] int Weight);
}
