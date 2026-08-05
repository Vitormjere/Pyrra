using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Desafios {
    // gerencia categorias e desafios, validando acesso de admin nos endpoints
    public interface IChallengeCatalogService {
        Task<IReadOnlyList<ChallengeCategory>> GetCategoriesAsync(Guid adminUserId, CancellationToken cancellationToken = default);

        Task<ChallengeCategory> CreateCategoryAsync(
            Guid adminUserId, string name, string? description, string icon, ChallengeCategoryColor color,
            CancellationToken cancellationToken = default);

        Task<ChallengeCategory> UpdateCategoryAsync(
            Guid adminUserId, Guid categoryId, string name, string? description, string icon, ChallengeCategoryColor color,
            CancellationToken cancellationToken = default);

        // impede remover categorias com desafios vinculados
        Task DeleteCategoryAsync(Guid adminUserId, Guid categoryId, CancellationToken cancellationToken = default);

        // lista todos ou filtra desafios por categoria
        Task<IReadOnlyList<Challenge>> GetChallengesAsync(Guid adminUserId, Guid? categoryId = null, CancellationToken cancellationToken = default);

        Task<Challenge> CreateChallengeAsync(
            Guid adminUserId, Guid categoryId, string title, string? description, int points, DateTime? deadline,
            CancellationToken cancellationToken = default);

        Task<Challenge> UpdateChallengeAsync(
            Guid adminUserId, Guid challengeId, Guid categoryId, string title, string? description, int points, DateTime? deadline,
            CancellationToken cancellationToken = default);

        Task DeleteChallengeAsync(Guid adminUserId, Guid challengeId, CancellationToken cancellationToken = default);
    }
}
