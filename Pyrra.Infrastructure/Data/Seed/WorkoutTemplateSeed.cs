using System;
using System.Collections.Generic;
using System.Linq;
using Pyrra.Domain.Common;
using Pyrra.Domain.Treinos;

namespace Pyrra.Infrastructure.Data.Seed {
    /// <summary>
    /// Fonte de verdade dos templates de treino. Os dados vivem aqui em forma declarativa e são
    /// achatados nas três listas de entidades que o HasData do PyrraDbContext semeia. Para
    /// acrescentar um template no futuro: some um item em <see cref="Definitions"/> e gere uma nova
    /// migration — os GUIDs são derivados da posição, então os existentes não mudam.
    ///
    /// Reps são inteiros: isométricos por tempo ("Prancha 3x40s") entram como Sets=3, Reps=40.
    /// Dia de descanso tem Label "Descanso" e nenhum exercício.
    /// </summary>
    internal static class WorkoutTemplateSeed {
        private sealed record Ex(string Name, int? Sets, int? Reps);
        private sealed record Day(WeekDay DayOfWeek, string Label, Ex[] Exercises);
        private sealed record Tpl(string Name, string Description, int TrainingDays, bool IsCustom, Day[] Days);

        private static Day Rest(WeekDay day) => new(day, "Descanso", Array.Empty<Ex>());

        private static readonly Tpl[] Definitions = {
            new("Full Body",
                "Corpo inteiro a cada treino. Ideal para quem treina 3x na semana.",
                TrainingDays: 3, IsCustom: false, Days: new[] {
                    new Day(WeekDay.Segunda, "Full Body A", new[] {
                        new Ex("Agachamento", 4, 10),
                        new Ex("Supino reto", 4, 10),
                        new Ex("Remada curvada", 4, 10),
                        new Ex("Desenvolvimento ombro", 3, 12),
                        new Ex("Rosca direta", 3, 12),
                        new Ex("Tríceps corda", 3, 12),
                    }),
                    Rest(WeekDay.Terca),
                    new Day(WeekDay.Quarta, "Full Body B", new[] {
                        new Ex("Levantamento terra", 4, 8),
                        new Ex("Supino inclinado", 4, 10),
                        new Ex("Puxada frente", 4, 10),
                        new Ex("Elevação lateral", 3, 15),
                        new Ex("Abdômen", 3, 15),
                    }),
                    Rest(WeekDay.Quinta),
                    new Day(WeekDay.Sexta, "Full Body C", new[] {
                        new Ex("Leg press", 4, 12),
                        new Ex("Crucifixo", 3, 12),
                        new Ex("Remada baixa", 4, 10),
                        new Ex("Panturrilha", 4, 15),
                        new Ex("Prancha", 3, 40),
                    }),
                    Rest(WeekDay.Sabado),
                    Rest(WeekDay.Domingo),
                }),

            new("Upper/Lower/Full",
                "Superior, inferior e um dia de corpo inteiro. 3x na semana.",
                TrainingDays: 3, IsCustom: false, Days: new[] {
                    new Day(WeekDay.Segunda, "Superior", new[] {
                        new Ex("Supino reto", 4, 10),
                        new Ex("Remada curvada", 4, 10),
                        new Ex("Desenvolvimento", 3, 12),
                        new Ex("Rosca direta", 3, 12),
                        new Ex("Tríceps corda", 3, 12),
                    }),
                    Rest(WeekDay.Terca),
                    new Day(WeekDay.Quarta, "Inferior", new[] {
                        new Ex("Agachamento", 4, 10),
                        new Ex("Levantamento terra romeno", 4, 10),
                        new Ex("Leg press", 3, 12),
                        new Ex("Panturrilha", 4, 15),
                        new Ex("Abdômen", 3, 15),
                    }),
                    Rest(WeekDay.Quinta),
                    new Day(WeekDay.Sexta, "Full Body", new[] {
                        new Ex("Supino inclinado", 3, 10),
                        new Ex("Puxada frente", 3, 10),
                        new Ex("Agachamento livre", 3, 10),
                        new Ex("Elevação lateral", 3, 15),
                        new Ex("Prancha", 3, 40),
                    }),
                    Rest(WeekDay.Sabado),
                    Rest(WeekDay.Domingo),
                }),

            new("Upper/Lower",
                "Alterna superior e inferior em 4 treinos na semana.",
                TrainingDays: 4, IsCustom: false, Days: new[] {
                    new Day(WeekDay.Segunda, "Superior A", new[] {
                        new Ex("Supino reto", 4, 10),
                        new Ex("Remada curvada", 4, 10),
                        new Ex("Desenvolvimento", 3, 12),
                        new Ex("Rosca direta", 3, 12),
                        new Ex("Tríceps testa", 3, 12),
                    }),
                    new Day(WeekDay.Terca, "Inferior A", new[] {
                        new Ex("Agachamento", 4, 10),
                        new Ex("Cadeira extensora", 3, 12),
                        new Ex("Mesa flexora", 3, 12),
                        new Ex("Panturrilha", 4, 15),
                    }),
                    Rest(WeekDay.Quarta),
                    new Day(WeekDay.Quinta, "Superior B", new[] {
                        new Ex("Supino inclinado", 4, 10),
                        new Ex("Puxada frente", 4, 10),
                        new Ex("Elevação lateral", 3, 15),
                        new Ex("Rosca martelo", 3, 12),
                        new Ex("Tríceps corda", 3, 12),
                    }),
                    new Day(WeekDay.Sexta, "Inferior B", new[] {
                        new Ex("Levantamento terra", 4, 8),
                        new Ex("Leg press", 4, 12),
                        new Ex("Cadeira abdutora", 3, 15),
                        new Ex("Panturrilha", 4, 15),
                    }),
                    Rest(WeekDay.Sabado),
                    Rest(WeekDay.Domingo),
                }),

            new("Bro Split",
                "Um grupo muscular por dia, 5x na semana.",
                TrainingDays: 5, IsCustom: false, Days: new[] {
                    new Day(WeekDay.Segunda, "Peito", new[] {
                        new Ex("Supino reto", 4, 10),
                        new Ex("Supino inclinado", 4, 10),
                        new Ex("Crucifixo", 3, 12),
                        new Ex("Crossover", 3, 15),
                    }),
                    new Day(WeekDay.Terca, "Costas", new[] {
                        new Ex("Puxada frente", 4, 10),
                        new Ex("Remada curvada", 4, 10),
                        new Ex("Remada baixa", 3, 12),
                        new Ex("Pull-over", 3, 12),
                    }),
                    new Day(WeekDay.Quarta, "Perna", new[] {
                        new Ex("Agachamento", 4, 10),
                        new Ex("Leg press", 4, 12),
                        new Ex("Cadeira extensora", 3, 15),
                        new Ex("Mesa flexora", 3, 15),
                        new Ex("Panturrilha", 4, 15),
                    }),
                    new Day(WeekDay.Quinta, "Ombro", new[] {
                        new Ex("Desenvolvimento", 4, 10),
                        new Ex("Elevação lateral", 4, 15),
                        new Ex("Elevação frontal", 3, 12),
                        new Ex("Encolhimento", 3, 15),
                    }),
                    new Day(WeekDay.Sexta, "Braço", new[] {
                        new Ex("Rosca direta", 4, 12),
                        new Ex("Rosca martelo", 3, 12),
                        new Ex("Tríceps testa", 4, 12),
                        new Ex("Tríceps corda", 3, 15),
                    }),
                    Rest(WeekDay.Sabado),
                    Rest(WeekDay.Domingo),
                }),

            new("PPL 6x",
                "Push, pull e perna repetidos, 6x na semana.",
                TrainingDays: 6, IsCustom: false, Days: new[] {
                    new Day(WeekDay.Segunda, "Push", new[] {
                        new Ex("Supino reto", 4, 10),
                        new Ex("Desenvolvimento", 3, 12),
                        new Ex("Crucifixo", 3, 12),
                        new Ex("Tríceps corda", 3, 15),
                    }),
                    new Day(WeekDay.Terca, "Pull", new[] {
                        new Ex("Puxada frente", 4, 10),
                        new Ex("Remada curvada", 4, 10),
                        new Ex("Rosca direta", 3, 12),
                        new Ex("Face pull", 3, 15),
                    }),
                    new Day(WeekDay.Quarta, "Legs", new[] {
                        new Ex("Agachamento", 4, 10),
                        new Ex("Leg press", 4, 12),
                        new Ex("Mesa flexora", 3, 12),
                        new Ex("Panturrilha", 4, 15),
                    }),
                    new Day(WeekDay.Quinta, "Push", new[] {
                        new Ex("Supino inclinado", 4, 10),
                        new Ex("Elevação lateral", 3, 15),
                        new Ex("Crossover", 3, 15),
                        new Ex("Tríceps testa", 3, 12),
                    }),
                    new Day(WeekDay.Sexta, "Pull", new[] {
                        new Ex("Remada baixa", 4, 10),
                        new Ex("Puxada supinada", 3, 10),
                        new Ex("Rosca martelo", 3, 12),
                        new Ex("Pull-over", 3, 12),
                    }),
                    new Day(WeekDay.Sabado, "Legs", new[] {
                        new Ex("Levantamento terra", 4, 8),
                        new Ex("Cadeira extensora", 3, 15),
                        new Ex("Cadeira abdutora", 3, 15),
                        new Ex("Panturrilha", 4, 15),
                    }),
                    Rest(WeekDay.Domingo),
                }),

            new("Foco em Perna",
                "Prioriza pernas com 3 treinos de membro inferior.",
                TrainingDays: 4, IsCustom: false, Days: new[] {
                    new Day(WeekDay.Segunda, "Perna A (quadríceps)", new[] {
                        new Ex("Agachamento", 4, 10),
                        new Ex("Leg press", 4, 12),
                        new Ex("Cadeira extensora", 4, 15),
                        new Ex("Avanço", 3, 12),
                    }),
                    new Day(WeekDay.Terca, "Superior leve", new[] {
                        new Ex("Supino", 3, 12),
                        new Ex("Puxada frente", 3, 12),
                        new Ex("Desenvolvimento", 3, 12),
                    }),
                    new Day(WeekDay.Quarta, "Perna B (posterior/glúteo)", new[] {
                        new Ex("Levantamento terra romeno", 4, 10),
                        new Ex("Mesa flexora", 4, 12),
                        new Ex("Elevação pélvica", 4, 12),
                        new Ex("Panturrilha", 4, 15),
                    }),
                    Rest(WeekDay.Quinta),
                    new Day(WeekDay.Sexta, "Perna C (geral)", new[] {
                        new Ex("Agachamento búlgaro", 3, 12),
                        new Ex("Cadeira abdutora", 3, 15),
                        new Ex("Panturrilha", 4, 15),
                        new Ex("Abdômen", 3, 15),
                    }),
                    Rest(WeekDay.Sabado),
                    Rest(WeekDay.Domingo),
                }),

            new("Foco em Superior",
                "Prioriza peito, costas, ombro e braço. 4x na semana.",
                TrainingDays: 4, IsCustom: false, Days: new[] {
                    new Day(WeekDay.Segunda, "Peito/Tríceps", new[] {
                        new Ex("Supino reto", 4, 10),
                        new Ex("Supino inclinado", 3, 10),
                        new Ex("Crossover", 3, 15),
                        new Ex("Tríceps corda", 3, 15),
                    }),
                    new Day(WeekDay.Terca, "Costas/Bíceps", new[] {
                        new Ex("Puxada frente", 4, 10),
                        new Ex("Remada curvada", 4, 10),
                        new Ex("Rosca direta", 3, 12),
                        new Ex("Rosca martelo", 3, 12),
                    }),
                    Rest(WeekDay.Quarta),
                    new Day(WeekDay.Quinta, "Ombro/Braço", new[] {
                        new Ex("Desenvolvimento", 4, 10),
                        new Ex("Elevação lateral", 4, 15),
                        new Ex("Rosca scott", 3, 12),
                        new Ex("Tríceps testa", 3, 12),
                    }),
                    new Day(WeekDay.Sexta, "Perna (manutenção)", new[] {
                        new Ex("Agachamento", 3, 12),
                        new Ex("Leg press", 3, 12),
                        new Ex("Panturrilha", 3, 15),
                    }),
                    Rest(WeekDay.Sabado),
                    Rest(WeekDay.Domingo),
                }),

            new("Treino em Casa",
                "Sem equipamento, usando o peso do corpo. 4x na semana.",
                TrainingDays: 4, IsCustom: false, Days: new[] {
                    new Day(WeekDay.Segunda, "Superior", new[] {
                        new Ex("Flexão de braço", 4, 12),
                        new Ex("Flexão diamante", 3, 10),
                        new Ex("Tríceps no banco/cadeira", 3, 12),
                        new Ex("Prancha", 3, 40),
                    }),
                    new Day(WeekDay.Terca, "Inferior", new[] {
                        new Ex("Agachamento livre", 4, 15),
                        new Ex("Afundo", 3, 12),
                        new Ex("Elevação de panturrilha", 4, 20),
                        new Ex("Ponte de glúteo", 3, 15),
                    }),
                    Rest(WeekDay.Quarta),
                    new Day(WeekDay.Quinta, "Full Body", new[] {
                        new Ex("Burpee", 3, 10),
                        new Ex("Flexão de braço", 3, 12),
                        new Ex("Agachamento com salto", 3, 12),
                        new Ex("Prancha lateral", 3, 30),
                    }),
                    new Day(WeekDay.Sexta, "Core/Cardio", new[] {
                        new Ex("Mountain climber", 4, 20),
                        new Ex("Abdômen bicicleta", 3, 20),
                        new Ex("Prancha", 3, 40),
                        new Ex("Polichinelo", 3, 30),
                    }),
                    Rest(WeekDay.Sabado),
                    Rest(WeekDay.Domingo),
                }),

            new("Personalizado",
                "Monte sua própria semana do zero, dia a dia.",
                TrainingDays: 0, IsCustom: true, Days: Array.Empty<Day>()),
        };

        // Achatamento em entidades com GUIDs determinísticos. Os índices (1-based no template,
        // 0-based em dias e exercícios) compõem as chaves; acrescentar um template no fim não
        // desloca os anteriores.
        private static readonly (
            List<WorkoutTemplate> Templates,
            List<WorkoutTemplateDay> Days,
            List<WorkoutTemplateExercise> Exercises) Built = Build();

        public static IReadOnlyList<WorkoutTemplate> Templates => Built.Templates;
        public static IReadOnlyList<WorkoutTemplateDay> Days => Built.Days;
        public static IReadOnlyList<WorkoutTemplateExercise> Exercises => Built.Exercises;

        private static (List<WorkoutTemplate>, List<WorkoutTemplateDay>, List<WorkoutTemplateExercise>) Build() {
            var templates = new List<WorkoutTemplate>();
            var days = new List<WorkoutTemplateDay>();
            var exercises = new List<WorkoutTemplateExercise>();

            for (var t = 0; t < Definitions.Length; t++) {
                var def = Definitions[t];
                var templateNumber = t + 1;
                var templateId = DeterministicGuid.From($"workout-template-{templateNumber}");

                templates.Add(new WorkoutTemplate {
                    Id                  = templateId,
                    Name                = def.Name,
                    Description         = def.Description,
                    TrainingDaysPerWeek = def.TrainingDays,
                    Order               = templateNumber,
                    IsCustom            = def.IsCustom
                });

                for (var d = 0; d < def.Days.Length; d++) {
                    var dayDef = def.Days[d];
                    var dayId = DeterministicGuid.From($"workout-template-{templateNumber}-day-{d}");

                    days.Add(new WorkoutTemplateDay {
                        Id         = dayId,
                        TemplateId = templateId,
                        DayOfWeek  = dayDef.DayOfWeek,
                        Label      = dayDef.Label
                    });

                    for (var e = 0; e < dayDef.Exercises.Length; e++) {
                        var exDef = dayDef.Exercises[e];
                        exercises.Add(new WorkoutTemplateExercise {
                            Id            = DeterministicGuid.From($"workout-template-{templateNumber}-day-{d}-ex-{e}"),
                            TemplateDayId = dayId,
                            ExerciseName  = exDef.Name,
                            Sets          = exDef.Sets,
                            Reps          = exDef.Reps,
                            Order         = e
                        });
                    }
                }
            }

            return (templates, days, exercises);
        }
    }
}
