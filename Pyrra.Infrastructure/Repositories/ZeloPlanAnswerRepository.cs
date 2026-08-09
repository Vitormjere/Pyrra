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
    public class ZeloPlanAnswerRepository : IZeloPlanAnswerRepository {
        private readonly PyrraDbContext _context;

        public ZeloPlanAnswerRepository(PyrraDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<ZeloPlanAnswer>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            await _context.ZeloPlanAnswers
                .Where(a => a.SessionId == sessionId)
                .OrderBy(a => a.Order)
                .ToListAsync(cancellationToken);

        public async Task AddAsync(ZeloPlanAnswer answer, CancellationToken cancellationToken = default) {
            await _context.ZeloPlanAnswers.AddAsync(answer, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
