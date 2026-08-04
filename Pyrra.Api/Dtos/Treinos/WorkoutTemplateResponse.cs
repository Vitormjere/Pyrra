using System;
using System.Collections.Generic;
using System.Linq;
using Pyrra.Domain.Treinos;

namespace Pyrra.Api.Dtos.Treinos {
    // Exercício de um template. Espelha o WorkoutPlanExerciseResponse (nome, séries, reps), sem id
    // nem Type: o preview do template é só leitura, e todos os exercícios do catálogo são Academia.
    public record WorkoutTemplateExerciseResponse(
        string ExerciseName,
        int? Sets,
        int? Reps) {
        public static WorkoutTemplateExerciseResponse FromEntity(WorkoutTemplateExercise exercise) =>
            new(exercise.ExerciseName, exercise.Sets, exercise.Reps);
    }

    // DayOfWeek vai como nome ("Segunda"), mesmo critério dos demais enums. Label "Descanso" é
    // explícito no template (diferente do plano do usuário, onde descanso não existe).
    public record WorkoutTemplateDayResponse(
        string DayOfWeek,
        string Label,
        IEnumerable<WorkoutTemplateExerciseResponse> Exercises) {
        public static WorkoutTemplateDayResponse FromEntity(WorkoutTemplateDay day) =>
            new(day.DayOfWeek.ToString(),
                day.Label,
                day.Exercises.Select(WorkoutTemplateExerciseResponse.FromEntity));
    }

    // Um card do catálogo. restDaysPerWeek é derivado (7 − treino) para a tela não recalcular.
    // Days vem completo para o preview expansível; no "Personalizado" vem vazio.
    public record WorkoutTemplateResponse(
        Guid Id,
        string Name,
        string Description,
        int TrainingDaysPerWeek,
        int RestDaysPerWeek,
        bool IsCustom,
        IEnumerable<WorkoutTemplateDayResponse> Days) {
        public static WorkoutTemplateResponse FromEntity(WorkoutTemplate template) =>
            new(template.Id,
                template.Name,
                template.Description,
                template.TrainingDaysPerWeek,
                7 - template.TrainingDaysPerWeek,
                template.IsCustom,
                template.Days
                    .OrderBy(d => d.DayOfWeek)
                    .Select(WorkoutTemplateDayResponse.FromEntity));
    }
}
