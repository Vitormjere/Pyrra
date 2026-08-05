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
    public class ChallengeCategoryRepository : IChallengeCategoryRepository {
        private readonly PyrraDbContext _context;

        public ChallengeCategoryRepository(PyrraDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<ChallengeCategory>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _context.ChallengeCategories
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

        public Task<ChallengeCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.ChallengeCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        public async Task AddAsync(ChallengeCategory category, CancellationToken cancellationToken = default) {
            await _context.ChallengeCategories.AddAsync(category, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(ChallengeCategory category, CancellationToken cancellationToken = default) {
            _context.ChallengeCategories.Update(category);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(ChallengeCategory category, CancellationToken cancellationToken = default) {
            _context.ChallengeCategories.Remove(category);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
