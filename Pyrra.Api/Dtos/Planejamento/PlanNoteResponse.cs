using System;
using Pyrra.Domain.Planejamento;

namespace Pyrra.Api.Dtos.Planejamento {
    public record PlanNoteResponse(
        DateOnly Date,
        string Content,
        DateTime? UpdatedAt) {
        public static PlanNoteResponse FromEntity(DailyPlanNote note) =>
            new(note.Date, note.Content, note.UpdatedAt);

        // Dia sem nota devolve o mesmo formato com texto vazio e UpdatedAt nulo, em vez de 404:
        // o cliente é um bloco de notas, e "ainda não escreveu nada" é estado normal, não erro.
        // UpdatedAt nulo é o que distingue nota inexistente de nota salva vazia.
        public static PlanNoteResponse Empty(DateOnly date) =>
            new(date, string.Empty, null);
    }
}
