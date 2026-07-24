using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Planejamento;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class DailyPlanNoteRepository : IDailyPlanNoteRepository {
        private readonly PyrraDbContext _context;

        public DailyPlanNoteRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<DailyPlanNote?> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default) =>
            _context.DailyPlanNotes.FirstOrDefaultAsync(n => n.UserId == userId && n.Date == date, cancellationToken);

        public async Task<IReadOnlyList<DailyPlanNote>> GetRecentByUserAsync(Guid userId, DateOnly fromDate, CancellationToken cancellationToken = default) =>
            await _context.DailyPlanNotes
                .Where(n => n.UserId == userId && n.Date >= fromDate)
                .OrderByDescending(n => n.Date)
                .ToListAsync(cancellationToken);

        // Mesmo desenho do DailyScoreRepository.UpsertAsync: atualiza a instância já rastreada
        // pelo contexto, preservando o Id original, para o upsert nunca duplicar a linha do
        // par usuário+data.
        public async Task<DailyPlanNote> UpsertAsync(DailyPlanNote note, CancellationToken cancellationToken = default) {
            var existing = await GetByUserAndDateAsync(note.UserId, note.Date, cancellationToken);

            if (existing is null) {
                if (note.Id == Guid.Empty) {
                    note.Id = Guid.NewGuid();
                }
                await _context.DailyPlanNotes.AddAsync(note, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                return note;
            }

            existing.Content   = note.Content;
            existing.UpdatedAt = note.UpdatedAt;

            await _context.SaveChangesAsync(cancellationToken);
            return existing;
        }
    }
}
