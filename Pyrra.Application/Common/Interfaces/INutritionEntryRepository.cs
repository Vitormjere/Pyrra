using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Nutricao;

namespace Pyrra.Application.Common.Interfaces {
    public interface INutritionEntryRepository {
        // Necessário para a checagem de posse antes de remover: o service precisa carregar o item
        // para saber de quem ele é.
        Task<NutritionEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<NutritionEntry>> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);

        // Intervalo inclusivo nas duas pontas, como nos outros repositórios.
        Task<IReadOnlyList<NutritionEntry>> GetByUserAndDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

        Task AddAsync(NutritionEntry entry, CancellationToken cancellationToken = default);
        Task DeleteAsync(NutritionEntry entry, CancellationToken cancellationToken = default);
    }
}
