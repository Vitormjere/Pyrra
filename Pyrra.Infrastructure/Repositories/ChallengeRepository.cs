using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Desafios;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class ChallengeRepository : IChallengeRepository {
        private readonly PyrraDbContext _context;

        public ChallengeRepository(PyrraDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<Challenge>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _context.Challenges
                .OrderBy(c => c.Title)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<Challenge>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
            await _context.Challenges
                .Where(c => c.CategoryId == categoryId)
                .OrderBy(c => c.Title)
                .ToListAsync(cancellationToken);

        public Task<Challenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.Challenges.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        public Task<bool> AnyByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
            _context.Challenges.AnyAsync(c => c.CategoryId == categoryId, cancellationToken);

        public async Task AddAsync(Challenge challenge, CancellationToken cancellationToken = default) {
            await _context.Challenges.AddAsync(challenge, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Challenge challenge, CancellationToken cancellationToken = default) {
            _context.Challenges.Update(challenge);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Challenge challenge, CancellationToken cancellationToken = default) {
            _context.Challenges.Remove(challenge);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
