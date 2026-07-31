using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Nutricao;

namespace Pyrra.Application.Common.Interfaces {
    public interface INutritionEntryRepository {
        // Busca um lançamento de nutrição pelo identificador
        Task<NutritionEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // Retorna os lançamentos de nutrição do usuário na data informada
        Task<IReadOnlyList<NutritionEntry>> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);

        // Retorna os lançamentos de nutrição do usuário no intervalo informado
        Task<IReadOnlyList<NutritionEntry>> GetByUserAndDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

        Task AddAsync(NutritionEntry entry, CancellationToken cancellationToken = default);

        // Adiciona múltiplos lançamentos de nutrição
        Task AddRangeAsync(IReadOnlyList<NutritionEntry> entries, CancellationToken cancellationToken = default);

        Task UpdateAsync(NutritionEntry entry, CancellationToken cancellationToken = default);
        Task DeleteAsync(NutritionEntry entry, CancellationToken cancellationToken = default);
    }
}