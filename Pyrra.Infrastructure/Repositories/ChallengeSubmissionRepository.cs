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
    public class ChallengeSubmissionRepository : IChallengeSubmissionRepository {
        private readonly PyrraDbContext _context;

        public ChallengeSubmissionRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<ChallengeSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.ChallengeSubmissions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        public async Task<IReadOnlyList<ChallengeSubmission>> GetForUserAndTeamAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default) =>
            await _context.ChallengeSubmissions
                .Where(s => s.UserId == userId && s.TeamId == teamId)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<ChallengeSubmission>> GetForTeamAsync(Guid teamId, CancellationToken cancellationToken = default) =>
            await _context.ChallengeSubmissions
                .Where(s => s.TeamId == teamId)
                .ToListAsync(cancellationToken);

        public Task<ChallengeSubmission?> GetActiveForUserChallengeAsync(Guid userId, Guid challengeId, Guid teamId, CancellationToken cancellationToken = default) =>
            _context.ChallengeSubmissions.FirstOrDefaultAsync(s =>
                s.UserId == userId && s.ChallengeId == challengeId && s.TeamId == teamId &&
                s.Status != ChallengeSubmissionStatus.Recusado,
                cancellationToken);

        public async Task<IReadOnlyList<ChallengeSubmission>> GetPendingForTeamAsync(Guid teamId, CancellationToken cancellationToken = default) =>
            await _context.ChallengeSubmissions
                .Where(s => s.TeamId == teamId && s.Status == ChallengeSubmissionStatus.Pendente)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<ChallengeSubmission>> GetPendingForTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default) =>
            await _context.ChallengeSubmissions
                .Where(s => s.TournamentId == tournamentId && s.Status == ChallengeSubmissionStatus.Pendente)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<ChallengeSubmission>> GetApprovedForTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default) =>
            await _context.ChallengeSubmissions
                .Where(s => s.TournamentId == tournamentId && s.Status == ChallengeSubmissionStatus.Aprovado)
                .ToListAsync(cancellationToken);

        public async Task AddAsync(ChallengeSubmission submission, CancellationToken cancellationToken = default) {
            await _context.ChallengeSubmissions.AddAsync(submission, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(ChallengeSubmission submission, CancellationToken cancellationToken = default) {
            _context.ChallengeSubmissions.Update(submission);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
