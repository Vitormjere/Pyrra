using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Treinos;

namespace Pyrra.Application.Treinos {
    public class WorkoutTemplateService : IWorkoutTemplateService {
        private readonly IWorkoutTemplateRepository     _templateRepository;
        private readonly IWorkoutPlanDayRepository       _planDayRepository;
        private readonly IWorkoutPlanExerciseRepository  _planExerciseRepository;

        public WorkoutTemplateService(
            IWorkoutTemplateRepository     templateRepository,
            IWorkoutPlanDayRepository      planDayRepository,
            IWorkoutPlanExerciseRepository planExerciseRepository) {
            _templateRepository     = templateRepository;
            _planDayRepository      = planDayRepository;
            _planExerciseRepository = planExerciseRepository;
        }

        public Task<IReadOnlyList<WorkoutTemplate>> GetAllAsync(CancellationToken cancellationToken = default) =>
            _templateRepository.GetAllWithDetailsAsync(cancellationToken);

        public async Task ApplyAsync(Guid userId, Guid templateId, CancellationToken cancellationToken = default) {
            var template = await _templateRepository.GetByIdWithDetailsAsync(templateId, cancellationToken);
            if (template is null) {
                throw new NotFoundException($"Template de treino '{templateId}' não encontrado.");
            }

            // "Personalizado": nada a copiar. Não toca no plano do usuário — o front cuida de abrir
            // o editor manual. Sair antes de sobrescrever evita apagar a semana de quem escolheu
            // "montar do zero" já tendo algo montado.
            if (template.IsCustom) {
                return;
            }

            // Labels dos 7 dias: o UpsertMany atualiza os existentes e cria os que faltam, então o
            // dia de "Descanso" também recebe seu label — no plano do template o descanso é
            // deliberado, diferente do "sem plano" (null) do plano do usuário.
            var days = template.Days
                .OrderBy(d => d.DayOfWeek)
                .Select(d => new WorkoutPlanDay {
                    DayOfWeek = d.DayOfWeek,
                    Label     = d.Label
                })
                .ToList();

            await _planDayRepository.UpsertManyAsync(userId, days, cancellationToken);

            // Exercícios: cópia 1:1, todos como Academia (o catálogo é só de musculação). O Order
            // vem do template, preservando a sequência dentro do dia. ReplaceAll apaga os
            // anteriores primeiro, então dias que o template deixa vazios não herdam exercícios
            // de um plano anterior.
            var exercises = template.Days
                .SelectMany(day => day.Exercises
                    .OrderBy(e => e.Order)
                    .Select(e => new WorkoutPlanExercise {
                        Id           = Guid.NewGuid(),
                        UserId       = userId,
                        DayOfWeek    = day.DayOfWeek,
                        Type         = WorkoutType.Academia,
                        ExerciseName = e.ExerciseName,
                        Sets         = e.Sets,
                        Reps         = e.Reps,
                        Order        = e.Order
                    }))
                .ToList();

            await _planExerciseRepository.ReplaceAllForUserAsync(userId, exercises, cancellationToken);
        }
    }
}
