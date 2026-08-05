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
    public class TournamentRepository : ITournamentRepository {
        private readonly PyrraDbContext _context;

        public TournamentRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<Tournament?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.Tournaments.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        public Task<Tournament?> GetByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken = default) =>
            _context.Tournaments.FirstOrDefaultAsync(t => t.InviteToken == inviteToken, cancellationToken);

        public async Task<IReadOnlyList<Tournament>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _context.Tournaments
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<Tournament>> GetOwnedByUserAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
            await _context.Tournaments
                .Where(t => t.OwnerId == ownerId)
                .ToListAsync(cancellationToken);

        public async Task AddAsync(Tournament tournament, CancellationToken cancellationToken = default) {
            await _context.Tournaments.AddAsync(tournament, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Tournament tournament, CancellationToken cancellationToken = default) {
            _context.Tournaments.Update(tournament);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
