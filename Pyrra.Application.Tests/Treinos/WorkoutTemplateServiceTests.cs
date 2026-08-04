using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Treinos;
using Pyrra.Domain.Common;
using Pyrra.Domain.Treinos;
using Xunit;

namespace Pyrra.Application.Tests.Treinos {
    public class WorkoutTemplateServiceTests {
        private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Template representativo: dois dias de treino (Segunda, Quarta) e o resto descanso —
        // o suficiente para exercitar cópia, ordenação, dia de descanso e sobrescrita.
        private static WorkoutTemplate SampleTemplate(Guid? id = null, bool isCustom = false) {
            var templateId = id ?? Guid.Parse("22222222-2222-2222-2222-222222222222");
            var segundaId  = Guid.NewGuid();
            var quartaId   = Guid.NewGuid();

            return new WorkoutTemplate {
                Id                  = templateId,
                Name                = "Teste",
                Description         = "Template de teste",
                TrainingDaysPerWeek = 2,
                Order               = 1,
                IsCustom            = isCustom,
                Days = new List<WorkoutTemplateDay> {
                    new() {
                        Id = segundaId, TemplateId = templateId, DayOfWeek = WeekDay.Segunda, Label = "Push",
                        Exercises = new List<WorkoutTemplateExercise> {
                            new() { Id = Guid.NewGuid(), TemplateDayId = segundaId, ExerciseName = "Supino reto", Sets = 4, Reps = 10, Order = 0 },
                            new() { Id = Guid.NewGuid(), TemplateDayId = segundaId, ExerciseName = "Tríceps corda", Sets = 3, Reps = 15, Order = 1 },
                        }
                    },
                    new() { Id = Guid.NewGuid(), TemplateId = templateId, DayOfWeek = WeekDay.Terca, Label = "Descanso",
                            Exercises = new List<WorkoutTemplateExercise>() },
                    new() {
                        Id = quartaId, TemplateId = templateId, DayOfWeek = WeekDay.Quarta, Label = "Legs",
                        Exercises = new List<WorkoutTemplateExercise> {
                            new() { Id = Guid.NewGuid(), TemplateDayId = quartaId, ExerciseName = "Agachamento", Sets = 4, Reps = 10, Order = 0 },
                        }
                    },
                    new() { Id = Guid.NewGuid(), TemplateId = templateId, DayOfWeek = WeekDay.Quinta, Label = "Descanso",
                            Exercises = new List<WorkoutTemplateExercise>() },
                }
            };
        }

        private static (WorkoutTemplateService service, FakeWorkoutPlanDayRepository days, FakeWorkoutPlanExerciseRepository exercises)
            BuildService(params WorkoutTemplate[] templates) {
            var dayRepo = new FakeWorkoutPlanDayRepository();
            var exRepo  = new FakeWorkoutPlanExerciseRepository();
            var tplRepo = new FakeWorkoutTemplateRepository(templates);
            var service = new WorkoutTemplateService(tplRepo, dayRepo, exRepo);
            return (service, dayRepo, exRepo);
        }

        [Fact]
        public async Task ApplyAsync_CopiaExerciciosDoTemplateParaOPlanoDoUsuario() {
            var template = SampleTemplate();
            var (service, _, exRepo) = BuildService(template);

            await service.ApplyAsync(UserId, template.Id);

            // Três exercícios no total (2 na Segunda, 1 na Quarta); os dias de descanso não geram nada.
            Assert.Equal(3, exRepo.Exercises.Count);

            var segunda = exRepo.Exercises.Where(e => e.DayOfWeek == WeekDay.Segunda).OrderBy(e => e.Order).ToList();
            Assert.Equal(new[] { "Supino reto", "Tríceps corda" }, segunda.Select(e => e.ExerciseName));
            Assert.Equal(4, segunda[0].Sets);
            Assert.Equal(10, segunda[0].Reps);
            Assert.Equal(new[] { 0, 1 }, segunda.Select(e => e.Order));

            Assert.Single(exRepo.Exercises, e => e.DayOfWeek == WeekDay.Quarta);
            Assert.DoesNotContain(exRepo.Exercises, e => e.DayOfWeek == WeekDay.Terca);
        }

        [Fact]
        public async Task ApplyAsync_GravaLabelsDosDias_InclusiveDescanso() {
            var template = SampleTemplate();
            var (service, dayRepo, _) = BuildService(template);

            await service.ApplyAsync(UserId, template.Id);

            var byDay = dayRepo.Days.ToDictionary(d => d.DayOfWeek, d => d.Label);
            Assert.Equal("Push", byDay[WeekDay.Segunda]);
            Assert.Equal("Legs", byDay[WeekDay.Quarta]);
            // O descanso é gravado como label explícito, não deixado nulo.
            Assert.Equal("Descanso", byDay[WeekDay.Terca]);
            Assert.Equal("Descanso", byDay[WeekDay.Quinta]);
        }

        [Fact]
        public async Task ApplyAsync_ExerciciosCopiadosNaoTemVinculoComOTemplate() {
            var template = SampleTemplate();
            var (service, _, exRepo) = BuildService(template);

            var templateExerciseIds = template.Days.SelectMany(d => d.Exercises).Select(e => e.Id).ToHashSet();

            await service.ApplyAsync(UserId, template.Id);

            // Cada exercício copiado é uma linha nova, do usuário — id próprio, nunca o id do template.
            Assert.All(exRepo.Exercises, e => {
                Assert.Equal(UserId, e.UserId);
                Assert.DoesNotContain(e.Id, templateExerciseIds);
                Assert.NotEqual(Guid.Empty, e.Id);
            });
        }

        [Fact]
        public async Task ApplyAsync_SobrescrevePlanoExistente_SemDeixarExerciciosOrfaos() {
            var template = SampleTemplate();
            var (service, dayRepo, exRepo) = BuildService(template);

            // Semana anterior: exercícios em dias que o novo template não preenche (Sexta) e um dia
            // que ele transforma em descanso (Quinta). Nenhum pode sobrar depois da aplicação.
            exRepo.Exercises.AddRange(new[] {
                new WorkoutPlanExercise { Id = Guid.NewGuid(), UserId = UserId, DayOfWeek = WeekDay.Sexta, Type = WorkoutType.Academia, ExerciseName = "Antigo Sexta", Sets = 3, Reps = 10, Order = 0 },
                new WorkoutPlanExercise { Id = Guid.NewGuid(), UserId = UserId, DayOfWeek = WeekDay.Quinta, Type = WorkoutType.Academia, ExerciseName = "Antigo Quinta", Sets = 3, Reps = 10, Order = 0 },
                new WorkoutPlanExercise { Id = Guid.NewGuid(), UserId = UserId, DayOfWeek = WeekDay.Segunda, Type = WorkoutType.Academia, ExerciseName = "Antigo Segunda", Sets = 3, Reps = 10, Order = 0 },
            });
            dayRepo.Days.Add(new WorkoutPlanDay { Id = Guid.NewGuid(), UserId = UserId, DayOfWeek = WeekDay.Sexta, Label = "Peito antigo" });

            await service.ApplyAsync(UserId, template.Id);

            // Nada do plano anterior sobrevive: nem os exercícios órfãos, nem o antigo da Segunda.
            Assert.DoesNotContain(exRepo.Exercises, e => e.ExerciseName.StartsWith("Antigo"));
            Assert.DoesNotContain(exRepo.Exercises, e => e.DayOfWeek == WeekDay.Sexta);
            Assert.DoesNotContain(exRepo.Exercises, e => e.DayOfWeek == WeekDay.Quinta);
            Assert.Equal(3, exRepo.Exercises.Count);
        }

        [Fact]
        public async Task ApplyAsync_NaoDuplicaAoAplicarDuasVezes() {
            var template = SampleTemplate();
            var (service, _, exRepo) = BuildService(template);

            await service.ApplyAsync(UserId, template.Id);
            await service.ApplyAsync(UserId, template.Id);

            // A segunda aplicação substitui a primeira, não soma — continua com os mesmos 3.
            Assert.Equal(3, exRepo.Exercises.Count);
        }

        [Fact]
        public async Task ApplyAsync_TemplatePersonalizado_NaoAlteraOPlano() {
            var custom = SampleTemplate(Guid.Parse("33333333-3333-3333-3333-333333333333"), isCustom: true);
            var (service, dayRepo, exRepo) = BuildService(custom);

            // Plano já montado à mão: aplicar o "Personalizado" não pode apagá-lo.
            exRepo.Exercises.Add(new WorkoutPlanExercise { Id = Guid.NewGuid(), UserId = UserId, DayOfWeek = WeekDay.Segunda, Type = WorkoutType.Academia, ExerciseName = "Meu exercício", Sets = 3, Reps = 10, Order = 0 });
            dayRepo.Days.Add(new WorkoutPlanDay { Id = Guid.NewGuid(), UserId = UserId, DayOfWeek = WeekDay.Segunda, Label = "Meu dia" });

            await service.ApplyAsync(UserId, custom.Id);

            Assert.Single(exRepo.Exercises);
            Assert.Equal("Meu exercício", exRepo.Exercises[0].ExerciseName);
            Assert.Single(dayRepo.Days);
            Assert.Equal("Meu dia", dayRepo.Days[0].Label);
        }

        [Fact]
        public async Task ApplyAsync_TemplateInexistente_LancaNotFound() {
            var (service, _, _) = BuildService();

            await Assert.ThrowsAsync<NotFoundException>(
                () => service.ApplyAsync(UserId, Guid.Parse("44444444-4444-4444-4444-444444444444")));
        }

        [Fact]
        public async Task ApplyAsync_NaoAfetaOutroUsuario() {
            var template = SampleTemplate();
            var (service, dayRepo, exRepo) = BuildService(template);

            var outroUser = Guid.Parse("99999999-9999-9999-9999-999999999999");
            exRepo.Exercises.Add(new WorkoutPlanExercise { Id = Guid.NewGuid(), UserId = outroUser, DayOfWeek = WeekDay.Segunda, Type = WorkoutType.Academia, ExerciseName = "De outro", Sets = 3, Reps = 10, Order = 0 });

            await service.ApplyAsync(UserId, template.Id);

            // O exercício do outro usuário permanece intocado.
            Assert.Contains(exRepo.Exercises, e => e.UserId == outroUser && e.ExerciseName == "De outro");
            Assert.Equal(3, exRepo.Exercises.Count(e => e.UserId == UserId));
        }
    }
}
