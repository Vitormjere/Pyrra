using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Nutricao;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class NutritionPlanSeedLogRepository : INutritionPlanSeedLogRepository {
        private readonly PyrraDbContext _context;

        public NutritionPlanSeedLogRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<bool> HasSeededAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default) =>
            _context.NutritionPlanSeedLogs
                .AnyAsync(l => l.UserId == userId && l.Date == date, cancellationToken);

        // Tolera a marca já existente: duas requisições simultâneas para o mesmo dia podem
        // passar pelo HasSeeded antes de qualquer uma gravar, e o índice único barra a
        // segunda no banco. Como o efeito desejado — "está marcado" — já foi alcançado pela
        // primeira, engolir a violação é correto, não um remendo.
        public async Task MarkSeededAsync(Guid userId, DateOnly date, DateTime seededAt, CancellationToken cancellationToken = default) {
            var log = new NutritionPlanSeedLog {
                Id       = Guid.NewGuid(),
                UserId   = userId,
                Date     = date,
                SeededAt = seededAt
            };

            await _context.NutritionPlanSeedLogs.AddAsync(log, cancellationToken);

            try {
                await _context.SaveChangesAsync(cancellationToken);
            } catch (DbUpdateException) {
                // Solta a linha que falhou: mantida como Added, ela seria reenviada no
                // próximo SaveChanges do mesmo contexto e o erro voltaria.
                _context.Entry(log).State = EntityState.Detached;

                // Se a marca já existe, o objetivo foi cumprido por outra requisição. Só
                // então engolimos — qualquer outra falha de escrita continua subindo.
                var alreadyMarked = await _context.NutritionPlanSeedLogs
                    .AsNoTracking()
                    .AnyAsync(l => l.UserId == userId && l.Date == date, cancellationToken);

                if (!alreadyMarked) {
                    throw;
                }
            }
        }
    }
}
