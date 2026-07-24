using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Common;
using Pyrra.Domain.Nutricao;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class NutritionPlanItemRepository : INutritionPlanItemRepository {
        private readonly PyrraDbContext _context;

        public NutritionPlanItemRepository(PyrraDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<NutritionPlanItem>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            await _context.NutritionPlanItems
                .Where(i => i.UserId == userId)
                .OrderBy(i => i.DayOfWeek)
                .ThenBy(i => i.MealType)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<NutritionPlanItem>> GetByUserAndDayAsync(Guid userId, WeekDay dayOfWeek, CancellationToken cancellationToken = default) =>
            await _context.NutritionPlanItems
                .Where(i => i.UserId == userId && i.DayOfWeek == dayOfWeek)
                .OrderBy(i => i.MealType)
                .ToListAsync(cancellationToken);

        public Task<NutritionPlanItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.NutritionPlanItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        public async Task AddAsync(NutritionPlanItem item, CancellationToken cancellationToken = default) {
            await _context.NutritionPlanItems.AddAsync(item, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(NutritionPlanItem item, CancellationToken cancellationToken = default) {
            _context.NutritionPlanItems.Remove(item);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
