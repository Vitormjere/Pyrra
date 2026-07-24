using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Focos;

namespace Pyrra.Application.Focos {
    public interface IDailyFocusService {
        Task<DailyFocus> CreateAsync(Guid userId, string name, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DailyFocus>> GetAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<DailyFocus> UpdateWeightAsync(Guid userId, Guid focusId, int newWeight, CancellationToken cancellationToken = default);

        // Renomeia e RECATEGORIZA: Category/Weight são derivados do nome, então mudar o nome
        // os recalcula via FocusCategoryMapper. Mesma checagem de duplicidade da criação.
        Task<DailyFocus> UpdateNameAsync(Guid userId, Guid focusId, string newName, CancellationToken cancellationToken = default);

        Task DeactivateAsync(Guid userId, Guid focusId, CancellationToken cancellationToken = default);
    }
}
