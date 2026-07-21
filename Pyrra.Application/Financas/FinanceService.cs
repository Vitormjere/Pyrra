using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Financas;

namespace Pyrra.Application.Financas {
    public class FinanceService : IFinanceService {
        private readonly IFinanceCategoryRepository _categoryRepository;
        private readonly IFinanceEntryRepository    _entryRepository;
        private readonly IUserRepository            _userRepository;
        private readonly IClockService              _clock;

        public FinanceService(
            IFinanceCategoryRepository categoryRepository,
            IFinanceEntryRepository    entryRepository,
            IUserRepository            userRepository,
            IClockService              clock) {
            _categoryRepository = categoryRepository;
            _entryRepository    = entryRepository;
            _userRepository     = userRepository;
            _clock              = clock;
        }

        public Task<IReadOnlyList<FinanceCategory>> GetCategoriesAsync(Guid userId, CancellationToken cancellationToken = default) =>
            _categoryRepository.GetCategoriesForUserAsync(userId, cancellationToken);

        public async Task<FinanceCategory> CreateCategoryAsync(Guid userId, string name, CancellationToken cancellationToken = default) {
            // Normaliza antes de comparar E de gravar, mesmo critério do DailyFocusService.
            var normalizedName = name?.Trim();
            if (string.IsNullOrEmpty(normalizedName)) {
                throw new InvalidFinanceEntryException("O nome da categoria é obrigatório.");
            }

            await EnsureCategoryNameIsNotTakenAsync(userId, normalizedName, cancellationToken);

            var category = new FinanceCategory {
                Id        = Guid.NewGuid(),
                UserId    = userId,
                Name      = normalizedName,
                IsDefault = false
            };

            await _categoryRepository.AddCategoryAsync(category, cancellationToken);
            return category;
        }

        public async Task<FinanceEntry> CreateEntryAsync(Guid userId, Guid categoryId, decimal amount, FinanceEntryType type, DateOnly? date = null, string? description = null, CancellationToken cancellationToken = default) {
            // Valor é sempre positivo: o sinal vem do Type. Zero também não passa — lançamento
            // de zero não move saldo e só polui o extrato.
            if (amount <= 0) {
                throw new InvalidFinanceEntryException("O valor do lançamento deve ser maior que zero.");
            }

            if (!Enum.IsDefined(type)) {
                throw new InvalidFinanceEntryException($"Tipo de lançamento '{type}' não é válido.");
            }

            await EnsureCategoryIsVisibleAsync(userId, categoryId, cancellationToken);

            // Sem trava de data futura: lançamento agendado/previsto é uso legítimo de controle
            // financeiro. Data passada idem, que é o caso comum de registrar depois.
            var targetDate = date ?? await TodayAsync(userId, cancellationToken);

            var entry = new FinanceEntry {
                Id          = Guid.NewGuid(),
                UserId      = userId,
                CategoryId  = categoryId,
                Amount      = amount,
                Type        = type,
                Date        = targetDate,
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                CreatedAt   = _clock.UtcNow
            };

            await _entryRepository.AddEntryAsync(entry, cancellationToken);
            return entry;
        }

        // Saldo ATUAL: tudo que já aconteceu, até hoje inclusive. Lançamentos com data futura
        // (conta agendada, salário previsto) ficam de fora — já estão registrados, mas ainda não
        // entraram na conta, e somá-los mostraria um saldo que o usuário não tem.
        // Sem limite inferior: o acumulado começa no primeiro lançamento da história.
        public async Task<FinanceTotals> GetBalanceAsync(Guid userId, CancellationToken cancellationToken = default) {
            var today = await TodayAsync(userId, cancellationToken);
            return await _entryRepository.GetTotalsAsync(userId, null, today, cancellationToken);
        }

        public async Task<WeeklyFinanceSummary> GetWeeklySummaryAsync(Guid userId, DateOnly? weekStart = null, CancellationToken cancellationToken = default) {
            var start = WeekRange.StartOfWeek(weekStart ?? await TodayAsync(userId, cancellationToken));
            var end   = WeekRange.EndOfWeek(start);

            var entries = await _entryRepository.GetEntriesByUserAndDateRangeAsync(userId, start, end, cancellationToken);

            // Totais vêm do banco em vez de somar `entries` em memória: o mesmo recorte, calculado
            // de um jeito só, evita que a lista e os totais discordem se a query mudar.
            var totals = await _entryRepository.GetTotalsAsync(userId, start, end, cancellationToken);

            return new WeeklyFinanceSummary(start, end, entries, totals);
        }

        // Duplicidade considera TUDO que o usuário enxerga — as padrão do sistema e as dele.
        // Comparação em memória com OrdinalIgnoreCase para não depender do collation do banco,
        // mesmo critério do DailyFocusService.
        private async Task EnsureCategoryNameIsNotTakenAsync(Guid userId, string normalizedName, CancellationToken cancellationToken) {
            var categories = await _categoryRepository.GetCategoriesForUserAsync(userId, cancellationToken);

            var duplicated = categories.Any(c =>
                string.Equals(c.Name.Trim(), normalizedName, StringComparison.OrdinalIgnoreCase));

            if (duplicated) {
                throw new DuplicateFinanceCategoryException(normalizedName);
            }
        }

        // Categoria de OUTRO usuário é tratada como inexistente: mesmo NotFoundException genérico
        // dos outros módulos, para não revelar que o id existe mas pertence a outra pessoa.
        private async Task EnsureCategoryIsVisibleAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken) {
            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);

            var visible = category is not null && (category.UserId is null || category.UserId == userId);
            if (!visible) {
                throw new NotFoundException($"Categoria '{categoryId}' não encontrada.");
            }
        }

        private async Task<DateOnly> TodayAsync(Guid userId, CancellationToken cancellationToken) {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }
            return _clock.TodayIn(user.Timezone);
        }
    }
}
