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
    public class TeamMemberScoreRepository : ITeamMemberScoreRepository {
        private readonly PyrraDbContext _context;

        public TeamMemberScoreRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<TeamMemberScore?> GetAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default) =>
            _context.TeamMemberScores.FirstOrDefaultAsync(s => s.TeamId == teamId && s.UserId == userId, cancellationToken);

        public async Task<IReadOnlyList<TeamMemberScore>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default) =>
            await _context.TeamMemberScores.Where(s => s.TeamId == teamId).ToListAsync(cancellationToken);

        public async Task AddAsync(TeamMemberScore score, CancellationToken cancellationToken = default) {
            await _context.TeamMemberScores.AddAsync(score, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(TeamMemberScore score, CancellationToken cancellationToken = default) {
            _context.TeamMemberScores.Update(score);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
