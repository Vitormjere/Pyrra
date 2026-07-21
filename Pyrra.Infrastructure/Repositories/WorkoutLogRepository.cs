using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Treinos;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class WorkoutLogRepository : IWorkoutLogRepository {
        private readonly PyrraDbContext _context;

        public WorkoutLogRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<WorkoutLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.WorkoutLogs.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        // Mais recentes primeiro: é a ordem que a lista de treinos mostra. CreatedAt desempata
        // dois treinos registrados na mesma data.
        public async Task<IReadOnlyList<WorkoutLog>> GetAllByUserIdAsync(Guid userId, WorkoutType? type = null, CancellationToken cancellationToken = default) {
            var query = _context.WorkoutLogs.Where(w => w.UserId == userId);

            if (type is not null) {
                query = query.Where(w => w.Type == type);
            }

            return await query
                .OrderByDescending(w => w.Date)
                .ThenByDescending(w => w.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        // Ordem CRESCENTE, ao contrário da listagem: o histórico de um exercício existe para ver
        // evolução de carga, e evolução se lê do mais antigo para o mais novo.
        //
        // A comparação de nome é feita em memória com OrdinalIgnoreCase, mesmo critério do
        // DailyFocusService, para não depender do collation do banco. Traz só os registros de
        // Academia do usuário — que é o universo onde ExerciseName existe.
        public async Task<IReadOnlyList<WorkoutLog>> GetByExerciseNameAsync(Guid userId, string exerciseName, CancellationToken cancellationToken = default) {
            var normalizedName = exerciseName.Trim();

            var academiaLogs = await _context.WorkoutLogs
                .Where(w => w.UserId == userId && w.Type == WorkoutType.Academia && w.ExerciseName != null)
                .ToListAsync(cancellationToken);

            return academiaLogs
                .Where(w => string.Equals(w.ExerciseName!.Trim(), normalizedName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(w => w.Date)
                .ThenBy(w => w.CreatedAt)
                .ToList();
        }

        public async Task AddAsync(WorkoutLog log, CancellationToken cancellationToken = default) {
            await _context.WorkoutLogs.AddAsync(log, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
