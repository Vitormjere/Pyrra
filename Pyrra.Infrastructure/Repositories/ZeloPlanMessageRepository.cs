using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Zelo;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class ZeloPlanMessageRepository : IZeloPlanMessageRepository {
        private readonly PyrraDbContext _context;

        public ZeloPlanMessageRepository(PyrraDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<ZeloPlanMessage>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            await _context.ZeloPlanMessages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync(cancellationToken);

        public async Task AddAsync(ZeloPlanMessage message, CancellationToken cancellationToken = default) {
            await _context.ZeloPlanMessages.AddAsync(message, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
