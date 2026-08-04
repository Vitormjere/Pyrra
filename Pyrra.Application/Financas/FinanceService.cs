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
            // normaliza antes de comparar e salvar
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

        public async Task DeleteCategoryAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default) {
            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);

            // retorna notfound para metas inválidas sem revelar existência
            if (category is null || category.UserId != userId) {
                throw new NotFoundException($"Categoria '{categoryId}' não encontrada.");
            }

            // impede apagar categorias usadas em lançamentos financeiros
            if (await _entryRepository.AnyByCategoryAsync(userId, categoryId, cancellationToken)) {
                throw new CategoryInUseException();
            }

            await _categoryRepository.DeleteCategoryAsync(category, cancellationToken);
        }

        public async Task<FinanceEntry> CreateEntryAsync(Guid userId, Guid categoryId, decimal amount, FinanceEntryType type, DateOnly? date = null, string? description = null, CancellationToken cancellationToken = default) {
            // exige valor positivo, evitando lançamentos sem efeito
            if (amount <= 0) {
                throw new InvalidFinanceEntryException("O valor do lançamento deve ser maior que zero.");
            }

            if (!Enum.IsDefined(type)) {
                throw new InvalidFinanceEntryException($"Tipo de lançamento '{type}' não é válido.");
            }

            await EnsureCategoryIsVisibleAsync(userId, categoryId, cancellationToken);

            // permite datas futuras e passadas para controle financeiro
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

        public async Task<FinanceEntry> UpdateEntryAsync(Guid userId, Guid entryId, Guid categoryId, decimal amount, FinanceEntryType type, DateOnly? date, string? description, CancellationToken cancellationToken = default) {
            var entry = await _entryRepository.GetByIdAsync(entryId, cancellationToken);
            if (entry is null || entry.UserId != userId) {
                throw new NotFoundException($"Lançamento '{entryId}' não encontrado.");
            }

            if (amount <= 0) {
                throw new InvalidFinanceEntryException("O valor do lançamento deve ser maior que zero.");
            }

            if (!Enum.IsDefined(type)) {
                throw new InvalidFinanceEntryException($"Tipo de lançamento '{type}' não é válido.");
            }

            await EnsureCategoryIsVisibleAsync(userId, categoryId, cancellationToken);

            // mantém a data original quando não informada na edição
            entry.CategoryId  = categoryId;
            entry.Amount      = amount;
            entry.Type        = type;
            entry.Date        = date ?? entry.Date;
            entry.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

            await _entryRepository.UpdateEntryAsync(entry, cancellationToken);
            return entry;
        }

        public async Task DeleteEntryAsync(Guid userId, Guid entryId, CancellationToken cancellationToken = default) {
            var entry = await _entryRepository.GetByIdAsync(entryId, cancellationToken);
            if (entry is null || entry.UserId != userId) {
                throw new NotFoundException($"Lançamento '{entryId}' não encontrado.");
            }

            await _entryRepository.DeleteEntryAsync(entry, cancellationToken);
        }

        // calcula saldo atual sem incluir lançamentos futuros
        public async Task<FinanceTotals> GetBalanceAsync(Guid userId, CancellationToken cancellationToken = default) {
            var today = await TodayAsync(userId, cancellationToken);
            return await _entryRepository.GetTotalsAsync(userId, null, today, cancellationToken);
        }

        public async Task<WeeklyFinanceSummary> GetWeeklySummaryAsync(Guid userId, DateOnly? weekStart = null, CancellationToken cancellationToken = default) {
            var start = WeekRange.StartOfWeek(weekStart ?? await TodayAsync(userId, cancellationToken));
            var end   = WeekRange.EndOfWeek(start);

            var entries = await _entryRepository.GetEntriesByUserAndDateRangeAsync(userId, start, end, cancellationToken);

            // calcula totais no banco para manter consistência com a consulta
            var totals = await _entryRepository.GetTotalsAsync(userId, start, end, cancellationToken);

            return new WeeklyFinanceSummary(start, end, entries, totals);
        }

        public async Task<IReadOnlyList<FinanceEntry>> GetEntriesForRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) {
            // retorna vazio quando o intervalo é inválido
            if (startDate > endDate) {
                return Array.Empty<FinanceEntry>();
            }

            return await _entryRepository.GetEntriesByUserAndDateRangeAsync(userId, startDate, endDate, cancellationToken);
        }

        // calcula a série diária com uma consulta de abertura e outra de lançamentos
        public async Task<IReadOnlyList<DailyBalance>> GetBalanceHistoryAsync(Guid userId, int days = 30, CancellationToken cancellationToken = default) {
            // garante uma janela válida de dias
            var window = Math.Max(1, days);

            var today = await TodayAsync(userId, cancellationToken);
            // considera hoje como parte da janela de dias
            var start = today.AddDays(-(window - 1));

            var opening = await _entryRepository.GetTotalsAsync(userId, null, start.AddDays(-1), cancellationToken);
            var entries = await _entryRepository.GetEntriesByUserAndDateRangeAsync(userId, start, today, cancellationToken);

            // calcula movimento diário conforme o tipo de lançamento
            var deltaByDate = entries
                .GroupBy(e => e.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(e => e.Type == FinanceEntryType.Entrada ? e.Amount : -e.Amount));

            var history = new List<DailyBalance>(window);
            var running = opening.Balance;

            for (var offset = 0; offset < window; offset++) {
                var date = start.AddDays(offset);
                if (deltaByDate.TryGetValue(date, out var delta)) {
                    running += delta;
                }
                history.Add(new DailyBalance(date, running));
            }

            // mantém o gráfico alinhado ao saldo atual sem incluir lançamentos futuros
            return history;
        }

        // verifica duplicidade entre categorias visíveis do usuário
        private async Task EnsureCategoryNameIsNotTakenAsync(Guid userId, string normalizedName, CancellationToken cancellationToken) {
            var categories = await _categoryRepository.GetCategoriesForUserAsync(userId, cancellationToken);

            var duplicated = categories.Any(c =>
                string.Equals(c.Name.Trim(), normalizedName, StringComparison.OrdinalIgnoreCase));

            if (duplicated) {
                throw new DuplicateFinanceCategoryException(normalizedName);
            }
        }

        // trata categorias de outros usuários como inexistentes
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
