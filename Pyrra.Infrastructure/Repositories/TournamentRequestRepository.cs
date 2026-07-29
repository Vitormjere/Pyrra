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
    public class TournamentRequestRepository : ITournamentRequestRepository {
        private readonly PyrraDbContext _context;

        public TournamentRequestRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<TournamentRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.TournamentRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        public async Task<IReadOnlyList<TournamentRequest>> GetPendingAsync(CancellationToken cancellationToken = default) =>
            await _context.TournamentRequests
                .Where(r => r.Status == TournamentRequestStatus.Pendente)
                .ToListAsync(cancellationToken);

        public async Task AddAsync(TournamentRequest request, CancellationToken cancellationToken = default) {
            await _context.TournamentRequests.AddAsync(request, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(TournamentRequest request, CancellationToken cancellationToken = default) {
            _context.TournamentRequests.Update(request);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
