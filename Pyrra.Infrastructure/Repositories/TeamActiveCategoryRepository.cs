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
    public class TeamActiveCategoryRepository : ITeamActiveCategoryRepository {
        private readonly PyrraDbContext _context;

        public TeamActiveCategoryRepository(PyrraDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<TeamActiveCategory>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default) =>
            await _context.TeamActiveCategories
                .Where(a => a.TeamId == teamId)
                .ToListAsync(cancellationToken);

        public Task<TeamActiveCategory?> GetAsync(Guid teamId, Guid categoryId, CancellationToken cancellationToken = default) =>
            _context.TeamActiveCategories
                .FirstOrDefaultAsync(a => a.TeamId == teamId && a.CategoryId == categoryId, cancellationToken);

        public async Task AddAsync(TeamActiveCategory activation, CancellationToken cancellationToken = default) {
            await _context.TeamActiveCategories.AddAsync(activation, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveAsync(TeamActiveCategory activation, CancellationToken cancellationToken = default) {
            _context.TeamActiveCategories.Remove(activation);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
