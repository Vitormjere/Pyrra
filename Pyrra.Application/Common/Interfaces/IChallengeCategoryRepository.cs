using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Common.Interfaces {
    public interface IChallengeCategoryRepository {
        Task<IReadOnlyList<ChallengeCategory>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<ChallengeCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(ChallengeCategory category, CancellationToken cancellationToken = default);
        Task UpdateAsync(ChallengeCategory category, CancellationToken cancellationToken = default);
        Task DeleteAsync(ChallengeCategory category, CancellationToken cancellationToken = default);
    }
}
