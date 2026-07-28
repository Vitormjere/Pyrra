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
    public class TeamInviteRepository : ITeamInviteRepository {
        private readonly PyrraDbContext _context;

        public TeamInviteRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<TeamInvite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.TeamInvites.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        public Task<TeamInvite?> GetByTeamAndInviteeAsync(Guid teamId, Guid inviteeId, CancellationToken cancellationToken = default) =>
            _context.TeamInvites.FirstOrDefaultAsync(i => i.TeamId == teamId && i.InviteeId == inviteeId, cancellationToken);

        public async Task<IReadOnlyList<TeamInvite>> GetPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default) =>
            await _context.TeamInvites
                .Where(i => i.Status == TeamInviteStatus.Pendente && i.InviteeId == userId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(cancellationToken);

        public Task<int> CountPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default) =>
            _context.TeamInvites.CountAsync(
                i => i.Status == TeamInviteStatus.Pendente && i.InviteeId == userId,
                cancellationToken);

        public async Task AddAsync(TeamInvite invite, CancellationToken cancellationToken = default) {
            await _context.TeamInvites.AddAsync(invite, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(TeamInvite invite, CancellationToken cancellationToken = default) {
            _context.TeamInvites.Update(invite);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveAllForTeamAsync(Guid teamId, CancellationToken cancellationToken = default) {
            var invites = await _context.TeamInvites.Where(i => i.TeamId == teamId).ToListAsync(cancellationToken);
            if (invites.Count == 0) {
                return;
            }

            _context.TeamInvites.RemoveRange(invites);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
