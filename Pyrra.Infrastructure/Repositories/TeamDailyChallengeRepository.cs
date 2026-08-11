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
    public class TeamDailyChallengeRepository : ITeamDailyChallengeRepository {
        private readonly PyrraDbContext _context;

        public TeamDailyChallengeRepository(PyrraDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<TeamDailyChallenge>> GetForTeamAndDateAsync(Guid teamId, DateOnly date, CancellationToken cancellationToken = default) =>
            await _context.TeamDailyChallenges
                .Where(d => d.TeamId == teamId && d.Date == date)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<Guid>> GetTeamIdsWithEntriesForDateAsync(DateOnly date, CancellationToken cancellationToken = default) =>
            await _context.TeamDailyChallenges
                .Where(d => d.Date == date)
                .Select(d => d.TeamId)
                .Distinct()
                .ToListAsync(cancellationToken);

        public async Task AddRangeAsync(IEnumerable<TeamDailyChallenge> entries, CancellationToken cancellationToken = default) {
            await _context.TeamDailyChallenges.AddRangeAsync(entries, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
