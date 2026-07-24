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
            // Normaliza antes de comparar E de gravar: " Beber Agua " e "beber agua" são o mesmo foco.
            var normalizedName = name.Trim();

            await EnsureNameIsNotTakenAsync(userId, normalizedName, cancellationToken);

            var (category, weight) = FocusCategoryMapper.Categorize(normalizedName);

            var focus = new DailyFocus {
                Id        = Guid.NewGuid(),
                UserId    = userId,
                Name      = normalizedName,
                Category  = category,
                Weight    = weight,
                Active    = true,
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

        public async Task<DailyFocus> UpdateNameAsync(Guid userId, Guid focusId, string newName, CancellationToken cancellationToken = default) {
            var focus = await GetOwnedFocusAsync(userId, focusId, cancellationToken);

            var normalizedName = newName?.Trim();
            if (string.IsNullOrEmpty(normalizedName)) {
                throw new InvalidFocusNameException();
            }

            // Renomear para o mesmo nome (só mudou capitalização/espaços) é permitido e não
            // dispara duplicidade — o próprio foco é excluído da checagem.
            await EnsureNameIsNotTakenAsync(userId, normalizedName, cancellationToken, focusId);

            // Category e Weight são função do nome: mudou o nome, recalcula os dois. O peso
            // congelado nos FocusLog passados não é afetado — só o peso atual do foco muda.
            var (category, weight) = FocusCategoryMapper.Categorize(normalizedName);

            focus.Name     = normalizedName;
            focus.Category = category;
            focus.Weight   = weight;

            await _repository.UpdateAsync(focus, cancellationToken);
            return focus;
        }

        public async Task DeactivateAsync(Guid userId, Guid focusId, CancellationToken cancellationToken = default) {
            var focus = await GetOwnedFocusAsync(userId, focusId, cancellationToken);

            focus.Active = false;
            await _repository.UpdateAsync(focus, cancellationToken);
        }

        // A duplicidade só considera focos ATIVOS: um foco desativado não bloqueia recriar o mesmo
        // nome depois. Comparação em memória com OrdinalIgnoreCase para não depender do collation do banco.
        private async Task EnsureNameIsNotTakenAsync(Guid userId, string normalizedName, CancellationToken cancellationToken, Guid? excludeFocusId = null) {
            var focuses = await _repository.GetAllByUserIdAsync(userId, cancellationToken);

            var duplicated = focuses.Any(f =>
                f.Active
                && f.Id != excludeFocusId
                && string.Equals(f.Name.Trim(), normalizedName, StringComparison.OrdinalIgnoreCase));

            if (duplicated) {
                throw new DuplicateFocusException(normalizedName);
            }
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
