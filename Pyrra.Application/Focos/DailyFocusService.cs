using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Focos;

namespace Pyrra.Application.Focos {
    public class DailyFocusService : IDailyFocusService {
        private readonly IDailyFocusRepository _repository;

        public DailyFocusService(IDailyFocusRepository repository) {
            _repository = repository;
        }

        public async Task<DailyFocus> CreateAsync(Guid userId, string name, CancellationToken cancellationToken = default) {
            var (category, weight) = FocusCategoryMapper.Categorize(name);

            var focus = new DailyFocus {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = name,
                Category = category,
                Weight = weight,
                Active = true,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(focus, cancellationToken);
            return focus;
        }

        public async Task<IReadOnlyList<DailyFocus>> GetAllForUserAsync(Guid userId, CancellationToken cancellationToken = default) {
            var focuses = await _repository.GetAllByUserIdAsync(userId, cancellationToken);
            return focuses.Where(f => f.Active).ToList();
        }

        public async Task<DailyFocus> UpdateWeightAsync(Guid userId, Guid focusId, int newWeight, CancellationToken cancellationToken = default) {
            var focus = await GetOwnedFocusAsync(userId, focusId, cancellationToken);

            focus.Weight = newWeight;
            await _repository.UpdateAsync(focus, cancellationToken);
            return focus;
        }

        public async Task DeactivateAsync(Guid userId, Guid focusId, CancellationToken cancellationToken = default) {
            var focus = await GetOwnedFocusAsync(userId, focusId, cancellationToken);

            focus.Active = false;
            await _repository.UpdateAsync(focus, cancellationToken);
        }

        // Carrega o foco garantindo a posse: se não existir OU pertencer a outro usuário,
        // lança o mesmo NotFoundException genérico — nunca revela que o foco existe mas é de outro dono.
        private async Task<DailyFocus> GetOwnedFocusAsync(Guid userId, Guid focusId, CancellationToken cancellationToken) {
            var focus = await _repository.GetByIdAsync(focusId, cancellationToken);
            if (focus is null || focus.UserId != userId) {
                throw new NotFoundException($"Foco '{focusId}' não encontrado.");
            }
            return focus;
        }
    }
}
