using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Common.Interfaces {
    public interface IChallengeRepository {
        Task<IReadOnlyList<Challenge>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Challenge>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
        Task<Challenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // Verifica se a categoria pode ser removida
        Task<bool> AnyByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

        Task AddAsync(Challenge challenge, CancellationToken cancellationToken = default);
        Task UpdateAsync(Challenge challenge, CancellationToken cancellationToken = default);
        Task DeleteAsync(Challenge challenge, CancellationToken cancellationToken = default);
    }
}
