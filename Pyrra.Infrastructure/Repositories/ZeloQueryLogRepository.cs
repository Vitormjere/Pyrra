using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Zelo;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class ZeloQueryLogRepository : IZeloQueryLogRepository {
        private readonly PyrraDbContext _context;

        public ZeloQueryLogRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<ZeloQueryLog?> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default) =>
            _context.ZeloQueryLogs.FirstOrDefaultAsync(l => l.UserId == userId && l.Date == date, cancellationToken);

        // Mesmo padrão do DailyScoreRepository: cria a linha do par usuário+data ou atualiza o Count
        // da já rastreada, preservando o Id — o índice único garante que nunca duplica.
        public async Task<ZeloQueryLog> UpsertAsync(ZeloQueryLog log, CancellationToken cancellationToken = default) {
            var existing = await GetByUserAndDateAsync(log.UserId, log.Date, cancellationToken);

            if (existing is null) {
                if (log.Id == Guid.Empty) {
                    log.Id = Guid.NewGuid();
                }
                await _context.ZeloQueryLogs.AddAsync(log, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                return log;
            }

            existing.Count = log.Count;
            await _context.SaveChangesAsync(cancellationToken);
            return existing;
        }
    }
}
