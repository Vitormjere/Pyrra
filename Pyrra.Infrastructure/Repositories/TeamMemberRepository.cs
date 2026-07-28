using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Comunidade;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class TeamMemberRepository : ITeamMemberRepository {
        private readonly PyrraDbContext _context;

        public TeamMemberRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<TeamMember?> GetAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default) =>
            _context.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userId, cancellationToken);

        public async Task<IReadOnlyList<TeamMember>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default) =>
            await _context.TeamMembers
                .Where(m => m.TeamId == teamId)
                .OrderBy(m => m.JoinedAt)
                .ToListAsync(cancellationToken);

        public Task<int> CountByTeamAsync(Guid teamId, CancellationToken cancellationToken = default) =>
            _context.TeamMembers.CountAsync(m => m.TeamId == teamId, cancellationToken);

        public Task<bool> ExistsAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default) =>
            _context.TeamMembers.AnyAsync(m => m.TeamId == teamId && m.UserId == userId, cancellationToken);

        public async Task AddAsync(TeamMember member, CancellationToken cancellationToken = default) {
            await _context.TeamMembers.AddAsync(member, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveAsync(TeamMember member, CancellationToken cancellationToken = default) {
            _context.TeamMembers.Remove(member);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveAllForTeamAsync(Guid teamId, CancellationToken cancellationToken = default) {
            var members = await _context.TeamMembers.Where(m => m.TeamId == teamId).ToListAsync(cancellationToken);
            if (members.Count == 0) {
                return;
            }

            _context.TeamMembers.RemoveRange(members);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
