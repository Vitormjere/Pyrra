using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Focos;

namespace Pyrra.Application.Tests.Comunidade {
    // Só GetByUserIdAsync é exercitado pelo RankingService (lê StreakStartDate depois do settle,
    // feito via IStreakService) — UpsertAsync não é chamado nos testes que usam este fake.
    internal sealed class FakeStreakRepository : IStreakRepository {
        private readonly Dictionary<Guid, Streak> _byUser = new();

        public void SetStreakStartDate(Guid userId, DateOnly? date) =>
            _byUser[userId] = new Streak { Id = Guid.NewGuid(), UserId = userId, StreakStartDate = date };

        public Task<Streak?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byUser.TryGetValue(userId, out var streak) ? streak : null);

        public Task<Streak> UpsertAsync(Streak streak, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Não usado pelos testes de ranking.");
    }
}
