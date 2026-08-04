using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Streaks;

namespace Pyrra.Application.Tests.Usuario {
    // só GetStatusAsync é exercitado pelo UserProfileService — os outros métodos lançam se algum dia forem chamados
    internal sealed class FakeStreakService : IStreakService {
        private readonly Dictionary<Guid, StreakStatusResult> _statusByUser = new();

        public void SetStatus(Guid userId, int current, int best) =>
            _statusByUser[userId] = new StreakStatusResult(current, best, 0, false, current, Array.Empty<MilestoneReached>());

        public Task<StreakStatusResult> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_statusByUser.TryGetValue(userId, out var status)
                ? status
                : new StreakStatusResult(0, 0, 0, false, 0, Array.Empty<MilestoneReached>()));

        public Task<StreakSettlementResult> SettleStreakAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Não usado pelos testes de perfil.");

        public Task<IReadOnlyList<PendingMilestoneItem>> GetPendingMilestonesAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Não usado pelos testes de perfil.");

        public Task<int> AcknowledgeMilestonesAsync(Guid userId, IReadOnlyCollection<Guid>? ids, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Não usado pelos testes de perfil.");

        public Task<IReadOnlyList<PendingFreezeUseItem>> GetPendingFreezeUsesAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Não usado pelos testes de perfil.");

        public Task<int> AcknowledgeFreezeUsesAsync(Guid userId, IReadOnlyCollection<Guid>? ids, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Não usado pelos testes de perfil.");
    }
}
