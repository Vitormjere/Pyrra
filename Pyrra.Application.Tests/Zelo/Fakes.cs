using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Application.Zelo;
using Pyrra.Domain.Zelo;

namespace Pyrra.Application.Tests.Zelo {
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

    internal sealed class FakeZeloPlanAssistant : IZeloPlanAssistant {
        public int CallCount { get; private set; }

        // configurável por teste: null usa um plano válido padrão
        public ZeloPlanGenerationResult? NextResult { get; set; }

        public Task<ZeloPlanGenerationResult> GeneratePlanAsync(
            string userContext, IReadOnlyList<ZeloPlanAnswer> answers, CancellationToken cancellationToken = default) {
            CallCount++;
            return Task.FromResult(NextResult ?? new ZeloPlanGenerationResult(true, MakeValidPlan(), string.Empty));
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
