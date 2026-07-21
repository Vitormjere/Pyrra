using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Focos;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class DailyScoreRepository : IDailyScoreRepository {
        private readonly PyrraDbContext _context;

        public DailyScoreRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<DailyScore?> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default) =>
            _context.DailyScores.FirstOrDefaultAsync(s => s.UserId == userId && s.Date == date, cancellationToken);

        public async Task<IReadOnlyList<DailyScore>> GetByUserAndDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
            await _context.DailyScores
                .Where(s => s.UserId == userId && s.Date >= startDate && s.Date <= endDate)
                .OrderBy(s => s.Date)
                .ToListAsync(cancellationToken);

        public async Task<DailyScore> UpsertAsync(DailyScore score, CancellationToken cancellationToken = default) {
            var existing = await GetByUserAndDateAsync(score.UserId, score.Date, cancellationToken);

            if (existing is null) {
                if (score.Id == Guid.Empty) {
                    score.Id = Guid.NewGuid();
                }
                await _context.DailyScores.AddAsync(score, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                return score;
            }

            // Atualiza a instância já rastreada pelo contexto, preservando o Id original —
            // assim o upsert nunca duplica a linha do par usuário+data.
            existing.PointsEarned   = score.PointsEarned;
            existing.PointsPossible = score.PointsPossible;
            existing.Percentage     = score.Percentage;
            existing.GoalMet        = score.GoalMet;

            await _context.SaveChangesAsync(cancellationToken);
            return existing;
        }
    }
}
