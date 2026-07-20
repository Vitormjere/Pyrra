using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Focos;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class DailyFocusRepository : IDailyFocusRepository {
        private readonly PyrraDbContext _context;

        public DailyFocusRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<DailyFocus?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.DailyFocuses.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        public async Task<IReadOnlyList<DailyFocus>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            await _context.DailyFocuses
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync(cancellationToken);

        public async Task AddAsync(DailyFocus focus, CancellationToken cancellationToken = default) {
            await _context.DailyFocuses.AddAsync(focus, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(DailyFocus focus, CancellationToken cancellationToken = default) {
            _context.DailyFocuses.Update(focus);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
