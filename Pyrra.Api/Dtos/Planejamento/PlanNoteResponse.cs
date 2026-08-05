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

        // quando não tem nota, retorna uma resposta vazia pra manter o mesmo formato
        public static PlanNoteResponse Empty(DateOnly date) =>
            new(date, string.Empty, null);
    }
}
