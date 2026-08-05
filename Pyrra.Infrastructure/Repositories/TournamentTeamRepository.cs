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
    public class TournamentTeamRepository : ITournamentTeamRepository {
        private readonly PyrraDbContext _context;

        public TournamentTeamRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<TournamentTeam?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.TournamentTeams.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        public async Task<IReadOnlyList<TournamentTeam>> GetActiveEntriesForTeamAsync(Guid teamId, CancellationToken cancellationToken = default) =>
            await _context.TournamentTeams
                .Where(t => t.TeamId == teamId && t.Status != TournamentTeamStatus.Recusado)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<TournamentTeam>> GetPendingForTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default) =>
            await _context.TournamentTeams
                .Where(t => t.TournamentId == tournamentId && t.Status == TournamentTeamStatus.Pendente)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<TournamentTeam>> GetApprovedForTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default) =>
            await _context.TournamentTeams
                .Where(t => t.TournamentId == tournamentId && t.Status == TournamentTeamStatus.Aprovado)
                .ToListAsync(cancellationToken);

        public async Task AddAsync(TournamentTeam tournamentTeam, CancellationToken cancellationToken = default) {
            await _context.TournamentTeams.AddAsync(tournamentTeam, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(TournamentTeam tournamentTeam, CancellationToken cancellationToken = default) {
            _context.TournamentTeams.Update(tournamentTeam);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
