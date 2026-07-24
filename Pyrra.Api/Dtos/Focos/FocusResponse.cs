using System;
using Pyrra.Domain.Focos;

namespace Pyrra.Api.Dtos.Focos {
    public record FocusResponse(
        Guid     Id,
        string   Name,
        string   Category,
        int      Weight,
        bool     Active,
        DateTime CreatedAt) {
        public static FocusResponse FromEntity(DailyFocus focus) =>
            new(focus.Id, focus.Name, focus.Category.ToString(), focus.Weight, focus.Active, focus.CreatedAt);
    }
}
