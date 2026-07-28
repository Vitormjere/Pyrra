using System;
using System.Diagnostics.Metrics;
using System.Runtime.Intrinsics.X86;
using Pyrra.Domain.Planejamento;

namespace Pyrra.Api.Dtos.Planejamento {
    public record PlanNoteResponse(
        DateOnly  Date,
        string    Content,
        DateTime? UpdatedAt) {
        public static PlanNoteResponse FromEntity(DailyPlanNote note) =>
        new(note.Date, note.Content, note.UpdatedAt);

        // Quando não tem nota, retorna uma resposta vazia para manter o mesmo format
        public static PlanNoteResponse Empty(DateOnly date) =>
            new(date, string.Empty, null);
    }
}
