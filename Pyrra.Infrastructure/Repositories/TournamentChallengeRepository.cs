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
    public class TournamentChallengeRepository : ITournamentChallengeRepository {
        private readonly PyrraDbContext _context;

        public TournamentChallengeRepository(PyrraDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<TournamentChallenge>> GetByTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default) =>
            await _context.TournamentChallenges
                .Where(l => l.TournamentId == tournamentId)
                .ToListAsync(cancellationToken);

        public Task<TournamentChallenge?> GetAsync(Guid tournamentId, Guid challengeId, CancellationToken cancellationToken = default) =>
            _context.TournamentChallenges
                .FirstOrDefaultAsync(l => l.TournamentId == tournamentId && l.ChallengeId == challengeId, cancellationToken);

        public async Task AddAsync(TournamentChallenge tournamentChallenge, CancellationToken cancellationToken = default) {
            await _context.TournamentChallenges.AddAsync(tournamentChallenge, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(TournamentChallenge tournamentChallenge, CancellationToken cancellationToken = default) {
            _context.TournamentChallenges.Update(tournamentChallenge);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveAsync(TournamentChallenge tournamentChallenge, CancellationToken cancellationToken = default) {
            _context.TournamentChallenges.Remove(tournamentChallenge);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
