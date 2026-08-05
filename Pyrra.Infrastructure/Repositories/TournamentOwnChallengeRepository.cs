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
    public class TournamentOwnChallengeRepository : ITournamentOwnChallengeRepository {
        private readonly PyrraDbContext _context;

        public TournamentOwnChallengeRepository(PyrraDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<TournamentOwnChallenge>> GetByTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default) =>
            await _context.TournamentOwnChallenges
                .Where(c => c.TournamentId == tournamentId)
                .OrderBy(c => c.Title)
                .ToListAsync(cancellationToken);

        public Task<TournamentOwnChallenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.TournamentOwnChallenges.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        public async Task AddAsync(TournamentOwnChallenge challenge, CancellationToken cancellationToken = default) {
            await _context.TournamentOwnChallenges.AddAsync(challenge, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(TournamentOwnChallenge challenge, CancellationToken cancellationToken = default) {
            _context.TournamentOwnChallenges.Update(challenge);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(TournamentOwnChallenge challenge, CancellationToken cancellationToken = default) {
            _context.TournamentOwnChallenges.Remove(challenge);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
