using System;
using System.ComponentModel.DataAnnotations;
using Pyrra.Domain.Desafios;

namespace Pyrra.Api.Dtos.Desafios {
    public record ChallengeResponse(
        Guid Id,
        Guid CategoryId,
        string Title,
        string? Description,
        int Points,
        DateTime? Deadline,
        DateTime CreatedAt,
        DateTime UpdatedAt) {
        public static ChallengeResponse FromEntity(Challenge c) => new(
            c.Id, c.CategoryId, c.Title, c.Description, c.Points, c.Deadline, c.CreatedAt, c.UpdatedAt);
    }

    public record CreateChallengeRequest(
        [Required] Guid? CategoryId,
        [Required] string Title,
        string? Description,
        [Required] int? Points,
        DateTime? Deadline);

    public record UpdateChallengeRequest(
        [Required] Guid? CategoryId,
        [Required] string Title,
        string? Description,
        [Required] int? Points,
        DateTime? Deadline);
}
