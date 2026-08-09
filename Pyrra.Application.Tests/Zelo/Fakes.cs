using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Application.Zelo;
using Pyrra.Domain.Common;
using Pyrra.Domain.Nutricao;
using Pyrra.Domain.Zelo;

namespace Pyrra.Application.Tests.Zelo {
    // mesmo padrão do FakeWorkoutPlanExerciseRepository (Treinos/Fakes.cs): ReplaceAllForUserAsync apaga tudo do usuário antes de gravar a lista nova
    internal sealed class FakeNutritionPlanItemRepository : INutritionPlanItemRepository {
        public readonly List<NutritionPlanItem> Items = new();

        public Task<IReadOnlyList<NutritionPlanItem>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NutritionPlanItem>>(Items.Where(i => i.UserId == userId).ToList());

        public Task<IReadOnlyList<NutritionPlanItem>> GetByUserAndDayAsync(Guid userId, WeekDay dayOfWeek, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NutritionPlanItem>>(
                Items.Where(i => i.UserId == userId && i.DayOfWeek == dayOfWeek).ToList());

        public Task<NutritionPlanItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        public Task AddAsync(NutritionPlanItem item, CancellationToken cancellationToken = default) {
            Items.Add(item);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(NutritionPlanItem item, CancellationToken cancellationToken = default) {
            Items.RemoveAll(i => i.Id == item.Id);
            return Task.CompletedTask;
        }

        public Task ReplaceAllForUserAsync(Guid userId, IReadOnlyList<NutritionPlanItem> items, CancellationToken cancellationToken = default) {
            Items.RemoveAll(i => i.UserId == userId);
            Items.AddRange(items);
            return Task.CompletedTask;
        }
    }
    internal sealed class FakeZeloPlanSessionRepository : IZeloPlanSessionRepository {
        public readonly List<ZeloPlanSession> Sessions = new();

        public Task<ZeloPlanSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Sessions.FirstOrDefault(s => s.Id == id));

        public Task<ZeloPlanSession?> GetActiveForUserAsync(Guid userId, DateTime now, CancellationToken cancellationToken = default) =>
            Task.FromResult(Sessions
                .Where(s => s.UserId == userId
                    && (s.Status == ZeloPlanSessionStatus.Coletando || s.Status == ZeloPlanSessionStatus.PlanoGerado)
                    && s.ExpiresAt > now)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefault());

        public Task AddAsync(ZeloPlanSession session, CancellationToken cancellationToken = default) {
            Sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ZeloPlanSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    internal sealed class FakeZeloPlanAnswerRepository : IZeloPlanAnswerRepository {
        public readonly List<ZeloPlanAnswer> Answers = new();

        public Task<IReadOnlyList<ZeloPlanAnswer>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ZeloPlanAnswer>>(
                Answers.Where(a => a.SessionId == sessionId).OrderBy(a => a.Order).ToList());

        public Task AddAsync(ZeloPlanAnswer answer, CancellationToken cancellationToken = default) {
            Answers.Add(answer);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeZeloPlanQueryLogRepository : IZeloPlanQueryLogRepository {
        public readonly List<ZeloPlanQueryLog> Logs = new();

        public Task<ZeloPlanQueryLog?> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default) =>
            Task.FromResult(Logs.FirstOrDefault(l => l.UserId == userId && l.Date == date));

        public Task<ZeloPlanQueryLog> UpsertAsync(ZeloPlanQueryLog log, CancellationToken cancellationToken = default) {
            var existing = Logs.FirstOrDefault(l => l.UserId == log.UserId && l.Date == log.Date);
            if (existing is null) {
                if (log.Id == Guid.Empty) log.Id = Guid.NewGuid();
                Logs.Add(log);
                return Task.FromResult(log);
            }

            existing.Count = log.Count;
            return Task.FromResult(existing);
        }
    }

    internal sealed class FakeZeloContextBuilder : IZeloContextBuilder {
        public string Context { get; set; } = "contexto de teste";

        public Task<string> BuildAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Context);
    }

    internal sealed class FakeZeloPlanMessageRepository : IZeloPlanMessageRepository {
        public readonly List<ZeloPlanMessage> Messages = new();

        public Task<IReadOnlyList<ZeloPlanMessage>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ZeloPlanMessage>>(
                Messages.Where(m => m.SessionId == sessionId).OrderBy(m => m.CreatedAt).ToList());

        public Task AddAsync(ZeloPlanMessage message, CancellationToken cancellationToken = default) {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeZeloPlanAssistant : IZeloPlanAssistant {
        public int CallCount { get; private set; }
        public int ChatCallCount { get; private set; }

        // configurável por teste: null usa um plano válido padrão
        public ZeloPlanGenerationResult? NextResult { get; set; }

        // configurável por teste: null usa uma resposta de sucesso padrão
        public ZeloAssistantResult? NextChatResult { get; set; }

        public Task<ZeloPlanGenerationResult> GeneratePlanAsync(
            string userContext, IReadOnlyList<ZeloPlanAnswer> answers, CancellationToken cancellationToken = default) {
            CallCount++;
            return Task.FromResult(NextResult ?? new ZeloPlanGenerationResult(true, MakeValidPlan(), string.Empty));
        }

        public Task<ZeloAssistantResult> ContinueChatAsync(
            string userContext, GeneratedPlan plan, IReadOnlyList<ZeloPlanMessage> history, string newMessage,
            CancellationToken cancellationToken = default) {
            ChatCallCount++;
            return Task.FromResult(NextChatResult ?? new ZeloAssistantResult(true, "Resposta de teste do Zelo."));
        }

        public static GeneratedPlan MakeValidPlan() {
            var workoutDays = Enum.GetValues<Pyrra.Domain.Common.WeekDay>()
                .Select(day => new GeneratedWorkoutDay(day, "Treino", new[] {
                    new GeneratedWorkoutExercise(Pyrra.Domain.Treinos.WorkoutType.Academia, "Supino reto", 4, 10, 0)
                }))
                .ToList();

            var nutritionDays = Enum.GetValues<Pyrra.Domain.Common.WeekDay>()
                .Select(day => new GeneratedNutritionDay(day, new[] {
                    new GeneratedNutritionItem(Pyrra.Domain.Nutricao.MealType.CafeDaManha, "Ovos", "3 unidades")
                }))
                .ToList();

            return new GeneratedPlan("Plano de teste.", workoutDays, nutritionDays);
        }
    }
}
