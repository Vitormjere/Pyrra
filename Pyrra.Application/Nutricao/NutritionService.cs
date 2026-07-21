using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Nutricao;

namespace Pyrra.Application.Nutricao {
    public class NutritionService : INutritionService {
        private static readonly MealType[] AllMeals = Enum.GetValues<MealType>();

        private readonly INutritionEntryRepository _repository;
        private readonly IUserRepository           _userRepository;
        private readonly IClockService             _clock;

        public NutritionService(
            INutritionEntryRepository repository,
            IUserRepository           userRepository,
            IClockService             clock) {
            _repository     = repository;
            _userRepository = userRepository;
            _clock          = clock;
        }

        public async Task<NutritionEntry> AddItemAsync(Guid userId, MealType mealType, string itemName, string quantity, DateOnly? date = null, CancellationToken cancellationToken = default) {
            // [Required] no DTO barra null e "", mas deixa passar "   ".
            var normalizedItemName = itemName?.Trim();
            if (string.IsNullOrEmpty(normalizedItemName)) {
                throw new InvalidNutritionEntryException("O nome do item é obrigatório.");
            }

            var normalizedQuantity = quantity?.Trim();
            if (string.IsNullOrEmpty(normalizedQuantity)) {
                throw new InvalidNutritionEntryException("A quantidade é obrigatória.");
            }

            if (!Enum.IsDefined(mealType)) {
                throw new InvalidNutritionEntryException($"Refeição '{mealType}' não é válida.");
            }

            // Sem trava de data futura, como nos demais módulos de registro: quem quiser deixar
            // o almoço de amanhã anotado não está fazendo nada inválido.
            var targetDate = date ?? await TodayAsync(userId, cancellationToken);

            var entry = new NutritionEntry {
                Id        = Guid.NewGuid(),
                UserId    = userId,
                Date      = targetDate,
                MealType  = mealType,
                ItemName  = normalizedItemName,
                Quantity  = normalizedQuantity,
                CreatedAt = _clock.UtcNow
            };

            await _repository.AddAsync(entry, cancellationToken);
            return entry;
        }

        public async Task<DayNutrition> GetForDayAsync(Guid userId, DateOnly? date = null, CancellationToken cancellationToken = default) {
            var targetDate = date ?? await TodayAsync(userId, cancellationToken);
            var entries    = await _repository.GetByUserAndDateAsync(userId, targetDate, cancellationToken);

            return BuildDay(targetDate, entries);
        }

        public async Task<WeekNutrition> GetForWeekAsync(Guid userId, DateOnly? weekStart = null, CancellationToken cancellationToken = default) {
            var start = WeekRange.StartOfWeek(weekStart ?? await TodayAsync(userId, cancellationToken));
            var end   = WeekRange.EndOfWeek(start);

            var entries = await _repository.GetByUserAndDateRangeAsync(userId, start, end, cancellationToken);

            // Uma query só para a semana inteira, agrupada em memória: sete consultas dia a dia
            // seriam sete idas ao banco para os mesmos dados.
            var byDate = entries.GroupBy(e => e.Date).ToDictionary(g => g.Key, g => (IReadOnlyList<NutritionEntry>)g.ToList());

            // Os sete dias sempre presentes, na ordem, mesmo os sem nenhum item: é o que faz a
            // aba da semana mostrar o PADRÃO — um dia em branco no meio da semana é informação.
            var days = Enumerable.Range(0, 7)
                .Select(offset => start.AddDays(offset))
                .Select(date => BuildDay(date, byDate.TryGetValue(date, out var dayEntries) ? dayEntries : Array.Empty<NutritionEntry>()))
                .ToList();

            return new WeekNutrition(start, end, days);
        }

        public async Task RemoveItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default) {
            var entry = await _repository.GetByIdAsync(itemId, cancellationToken);

            // Mesmo NotFoundException genérico dos outros módulos: item inexistente ou de outro
            // usuário são indistinguíveis para quem chama.
            if (entry is null || entry.UserId != userId) {
                throw new NotFoundException($"Item '{itemId}' não encontrado.");
            }

            await _repository.DeleteAsync(entry, cancellationToken);
        }

        // Monta as quatro refeições na ordem do enum, cada uma com os itens já ordenados pelo
        // repositório. Refeição sem item vira lista vazia, nunca grupo ausente.
        private static DayNutrition BuildDay(DateOnly date, IReadOnlyList<NutritionEntry> entries) {
            var meals = AllMeals
                .Select(meal => new MealGroup(
                    meal,
                    entries.Where(e => e.MealType == meal).ToList()))
                .ToList();

            return new DayNutrition(date, meals);
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
