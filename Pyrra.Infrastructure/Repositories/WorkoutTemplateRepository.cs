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
    public class WorkoutTemplateRepository : IWorkoutTemplateRepository {
        private readonly PyrraDbContext _context;

        public WorkoutTemplateRepository(PyrraDbContext context) {
            _context = context;
        }

        // AsNoTracking: o catálogo é só leitura, nunca editado pela aplicação. Ordena o catálogo,
        // os dias (Segunda→Domingo) e os exercícios (Order) já na consulta, para a tela e a cópia
        // receberem tudo na sequência certa sem reordenar depois.
        public async Task<IReadOnlyList<WorkoutTemplate>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default) =>
            await _context.WorkoutTemplates
                .AsNoTracking()
                .Include(t => t.Days.OrderBy(d => d.DayOfWeek))
                    .ThenInclude(d => d.Exercises.OrderBy(e => e.Order))
                .OrderBy(t => t.Order)
                .ToListAsync(cancellationToken);

        public Task<WorkoutTemplate?> GetByIdWithDetailsAsync(Guid templateId, CancellationToken cancellationToken = default) =>
            _context.WorkoutTemplates
                .AsNoTracking()
                .Include(t => t.Days.OrderBy(d => d.DayOfWeek))
                    .ThenInclude(d => d.Exercises.OrderBy(e => e.Order))
                .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);
    }
}
