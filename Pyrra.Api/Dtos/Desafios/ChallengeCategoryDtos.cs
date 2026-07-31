using System;
using System.ComponentModel.DataAnnotations;
using Pyrra.Domain.Desafios;

namespace Pyrra.Api.Dtos.Desafios {
    public record ChallengeCategoryResponse(
        Guid     Id,
        string   Name,
        string?  Description,
        string   Icon,
        string   Color,
        DateTime CreatedAt,
        DateTime UpdatedAt) {
        public static ChallengeCategoryResponse FromEntity(ChallengeCategory c) => new(
            c.Id, c.Name, c.Description, c.Icon, c.Color.ToString(), c.CreatedAt, c.UpdatedAt);
    }

    public record CreateChallengeCategoryRequest(
        [Required] string Name,
        string? Description,
        [Required] string Icon,
        [Required] string Color);

    public record UpdateChallengeCategoryRequest(
        [Required] string Name,
        string? Description,
        [Required] string Icon,
        [Required] string Color);
}
