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

        public async Task DeleteCategoryAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default) {
            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);

            // Inexistente ou de outro usuário: NotFound genérico. As padrão do sistema têm
            // UserId null, então também caem aqui — para elas é uma tentativa inválida, mas
            // não vale revelar que existem escondendo por trás do mesmo 404.
            if (category is null || category.UserId != userId) {
                throw new NotFoundException($"Categoria '{categoryId}' não encontrada.");
            }

            // Bloqueia se houver lançamentos: apagar deixaria referências órfãs, e o histórico
            // financeiro perderia a categoria de lançamentos passados. O usuário reatribui ou
            // apaga os lançamentos primeiro.
            if (await _entryRepository.AnyByCategoryAsync(userId, categoryId, cancellationToken)) {
                throw new CategoryInUseException();
            }

            await _categoryRepository.DeleteCategoryAsync(category, cancellationToken);
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

            // date nulo mantém a data atual, em vez de resetar para hoje: editar o valor de um
            // lançamento antigo não deve movê-lo para o dia de hoje sem o usuário pedir.
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

        public async Task<IReadOnlyList<FinanceEntry>> GetEntriesForRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) {
            // Intervalo invertido devolve vazio, mesmo critério das tarefas.
            if (startDate > endDate) {
                return Array.Empty<FinanceEntry>();
            }

            return await _entryRepository.GetEntriesByUserAndDateRangeAsync(userId, startDate, endDate, cancellationToken);
        }

        /// <summary>
        /// Série do saldo dia a dia, em DUAS consultas — não uma por dia.
        ///
        /// A ideia é que o saldo de um dia é o saldo do dia anterior mais o
        /// movimento daquele dia. Então basta:
        ///   1. um agregado no banco com tudo que aconteceu ANTES da janela
        ///      (o saldo de abertura), e
        ///   2. os lançamentos da janela, acumulados em memória.
        ///
        /// Trinta consultas "soma tudo até o dia X" repetiriam o mesmo histórico
        /// trinta vezes; carregar todos os lançamentos de sempre para somar aqui
        /// cresceria sem limite com o uso.
        /// </summary>
        public async Task<IReadOnlyList<DailyBalance>> GetBalanceHistoryAsync(Guid userId, int days = 30, CancellationToken cancellationToken = default) {
            // Piso de 1: days zero ou negativo produziria uma janela invertida.
            var window = Math.Max(1, days);

            var today = await TodayAsync(userId, cancellationToken);
            // -(window - 1) porque hoje é um dos pontos: 30 dias = hoje + 29 anteriores.
            var start = today.AddDays(-(window - 1));

            var opening = await _entryRepository.GetTotalsAsync(userId, null, start.AddDays(-1), cancellationToken);
            var entries = await _entryRepository.GetEntriesByUserAndDateRangeAsync(userId, start, today, cancellationToken);

            // Movimento líquido por dia: entrada soma, saída subtrai. O sinal vem do
            // Type porque Amount é sempre positivo.
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

            // A janela termina HOJE, então lançamentos futuros ficam de fora — mesmo
            // critério do GetBalanceAsync, para o último ponto do gráfico bater com o
            // saldo exibido no card.
            return history;
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
