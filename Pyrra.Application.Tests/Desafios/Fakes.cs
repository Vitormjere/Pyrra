using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Tests.Desafios {
    internal sealed class FakeChallengeCategoryRepository : IChallengeCategoryRepository {
        public readonly List<ChallengeCategory> Categories = new();

        public Task<IReadOnlyList<ChallengeCategory>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChallengeCategory>>(Categories.OrderBy(c => c.Name).ToList());

        public Task<ChallengeCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Categories.FirstOrDefault(c => c.Id == id));

        public Task AddAsync(ChallengeCategory category, CancellationToken cancellationToken = default) {
            Categories.Add(category);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ChallengeCategory category, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(ChallengeCategory category, CancellationToken cancellationToken = default) {
            Categories.RemoveAll(c => c.Id == category.Id);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeChallengeRepository : IChallengeRepository {
        public readonly List<Challenge> Challenges = new();

        public Task<IReadOnlyList<Challenge>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Challenge>>(Challenges.OrderBy(c => c.Title).ToList());

        public Task<IReadOnlyList<Challenge>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Challenge>>(Challenges.Where(c => c.CategoryId == categoryId).OrderBy(c => c.Title).ToList());

        public Task<Challenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Challenges.FirstOrDefault(c => c.Id == id));

        public Task<bool> AnyByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Challenges.Any(c => c.CategoryId == categoryId));

        public Task AddAsync(Challenge challenge, CancellationToken cancellationToken = default) {
            Challenges.Add(challenge);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Challenge challenge, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(Challenge challenge, CancellationToken cancellationToken = default) {
            Challenges.RemoveAll(c => c.Id == challenge.Id);
            return Task.CompletedTask;
        }
    }
}
