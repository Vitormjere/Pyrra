using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Achievements;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class AchievementRepository : IAchievementRepository {
        private readonly PyrraDbContext _context;

        public AchievementRepository(PyrraDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<Achievement>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _context.Achievements.ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<Achievement>> GetByTypeAsync(AchievementType type, CancellationToken cancellationToken = default) =>
            await _context.Achievements.Where(a => a.Type == type).ToListAsync(cancellationToken);
    }
}
