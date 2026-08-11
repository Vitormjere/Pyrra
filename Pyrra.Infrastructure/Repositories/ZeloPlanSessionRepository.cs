using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Zelo;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class ZeloPlanSessionRepository : IZeloPlanSessionRepository {
        private readonly PyrraDbContext _context;

        public ZeloPlanSessionRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<ZeloPlanSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.ZeloPlanSessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        public Task<ZeloPlanSession?> GetActiveForUserAsync(Guid userId, DateTime now, CancellationToken cancellationToken = default) =>
            _context.ZeloPlanSessions
                .Where(s => s.UserId == userId
                    && (s.Status == ZeloPlanSessionStatus.Coletando
                        || s.Status == ZeloPlanSessionStatus.PlanoGerado
                        || s.Status == ZeloPlanSessionStatus.Aplicada)
                    && s.ExpiresAt > now)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

        public async Task AddAsync(ZeloPlanSession session, CancellationToken cancellationToken = default) {
            await _context.ZeloPlanSessions.AddAsync(session, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(ZeloPlanSession session, CancellationToken cancellationToken = default) {
            _context.ZeloPlanSessions.Update(session);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
