using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Common;
using Pyrra.Domain.Nutricao;

namespace Pyrra.Application.Nutricao {
    public class NutritionService : INutritionService {
        private static readonly MealType[] AllMeals = Enum.GetValues<MealType>();

        private readonly INutritionEntryRepository       _repository;
        private readonly INutritionPlanItemRepository    _planRepository;
        private readonly INutritionPlanSeedLogRepository _seedLogRepository;
        private readonly IUserRepository                 _userRepository;
        private readonly IClockService                   _clock;

        public NutritionService(
            INutritionEntryRepository       repository,
            INutritionPlanItemRepository    planRepository,
            INutritionPlanSeedLogRepository seedLogRepository,
            IUserRepository                 userRepository,
            IClockService                   clock) {
            _repository        = repository;
            _planRepository    = planRepository;
            _seedLogRepository = seedLogRepository;
            _userRepository    = userRepository;
            _clock             = clock;
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

            // permite registros futuros, como nos demais módulos
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
            var today      = await TodayAsync(userId, cancellationToken);
            var targetDate = date ?? today;
            var entries    = await _repository.GetByUserAndDateAsync(userId, targetDate, cancellationToken);

            // copia o plano apenas hoje e uma vez por dia, sem reescrever histórico
            if (targetDate == today && !await _seedLogRepository.HasSeededAsync(userId, today, cancellationToken)) {
                var seeded = await SeedFromPlanAsync(userId, today, cancellationToken);
                if (seeded.Count > 0) {
                    // adiciona ao que já existe sem substituir dados manuais
                    entries = entries.Concat(seeded).OrderBy(e => e.MealType).ThenBy(e => e.CreatedAt).ToList();
                }
            }

            return BuildDay(targetDate, entries);
        }

        // copia o plano para itens reais, sem manter vínculo com a origem
        private async Task<IReadOnlyList<NutritionEntry>> SeedFromPlanAsync(Guid userId, DateOnly today, CancellationToken cancellationToken) {
            var planItems = await _planRepository.GetByUserAndDayAsync(userId, ToWeekDay(today), cancellationToken);

            // plano vazio não é marcado como semeado para permitir cópia depois
            if (planItems.Count == 0) {
                return Array.Empty<NutritionEntry>();
            }

            var now = _clock.UtcNow;
            var entries = planItems
                .Select(item => new NutritionEntry {
                    Id        = Guid.NewGuid(),
                    UserId    = userId,
                    Date      = today,
                    MealType  = item.MealType,
                    ItemName  = item.ItemName,
                    Quantity  = item.Quantity,
                    CreatedAt = now
                })
                .ToList();

            await _repository.AddRangeAsync(entries, cancellationToken);

            // marca como semeado só após copiar com sucesso
            await _seedLogRepository.MarkSeededAsync(userId, today, now, cancellationToken);

            return entries;
        }

        // converte o dia da semana do .NET para o padrão do domínio
        private static WeekDay ToWeekDay(DateOnly date) =>
            (WeekDay)(((int)date.DayOfWeek + 6) % 7);

        public async Task<WeekNutrition> GetForWeekAsync(Guid userId, DateOnly? weekStart = null, CancellationToken cancellationToken = default) {
            var start = WeekRange.StartOfWeek(weekStart ?? await TodayAsync(userId, cancellationToken));
            var end   = WeekRange.EndOfWeek(start);

            var entries = await _repository.GetByUserAndDateRangeAsync(userId, start, end, cancellationToken);

            // busca a semana inteira em uma única consulta e agrupa em memória
            var byDate = entries.GroupBy(e => e.Date).ToDictionary(g => g.Key, g => (IReadOnlyList<NutritionEntry>)g.ToList());

            // mantém os 7 dias na ordem, incluindo dias vazios, para a grade semanal completa
            var days = Enumerable.Range(0, 7)
                .Select(offset => start.AddDays(offset))
                .Select(date => BuildDay(date, byDate.TryGetValue(date, out var dayEntries) ? dayEntries : Array.Empty<NutritionEntry>()))
                .ToList();

            return new WeekNutrition(start, end, days);
        }

        public async Task<NutritionEntry> UpdateItemAsync(Guid userId, Guid itemId, string itemName, string quantity, CancellationToken cancellationToken = default) {
            var entry = await GetOwnedEntryAsync(userId, itemId, cancellationToken);

            var normalizedItemName = itemName?.Trim();
            if (string.IsNullOrEmpty(normalizedItemName)) {
                throw new InvalidNutritionEntryException("O nome do item é obrigatório.");
            }

            var normalizedQuantity = quantity?.Trim();
            if (string.IsNullOrEmpty(normalizedQuantity)) {
                throw new InvalidNutritionEntryException("A quantidade é obrigatória.");
            }

            entry.ItemName = normalizedItemName;
            entry.Quantity = normalizedQuantity;

            await _repository.UpdateAsync(entry, cancellationToken);
            return entry;
        }

        public async Task RemoveItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default) {
            var entry = await GetOwnedEntryAsync(userId, itemId, cancellationToken);
            await _repository.DeleteAsync(entry, cancellationToken);
        }

        // trata item inexistente ou de outro usuário como não encontrado
        private async Task<NutritionEntry> GetOwnedEntryAsync(Guid userId, Guid itemId, CancellationToken cancellationToken) {
            var entry = await _repository.GetByIdAsync(itemId, cancellationToken);
            if (entry is null || entry.UserId != userId) {
                throw new NotFoundException($"Item '{itemId}' não encontrado.");
            }
            return entry;
        }

        public async Task<IReadOnlyList<PlanDay>> GetPlanAsync(Guid userId, CancellationToken cancellationToken = default) {
            var items = await _planRepository.GetByUserAsync(userId, cancellationToken);

            // mantém a grade completa 7x4 com refeições vazias quando não há itens
            return Enum.GetValues<WeekDay>()
                .Select(day => new PlanDay(
                    day,
                    AllMeals
                        .Select(meal => new PlanMealGroup(
                            meal,
                            items.Where(i => i.DayOfWeek == day && i.MealType == meal).ToList()))
                        .ToList()))
                .ToList();
        }

        public async Task<NutritionPlanItem> AddPlanItemAsync(Guid userId, WeekDay day, MealType mealType, string itemName, string quantity, CancellationToken cancellationToken = default) {
            var normalizedItemName = itemName?.Trim();
            if (string.IsNullOrEmpty(normalizedItemName)) {
                throw new InvalidNutritionEntryException("O nome do item é obrigatório.");
            }

            var normalizedQuantity = quantity?.Trim();
            if (string.IsNullOrEmpty(normalizedQuantity)) {
                throw new InvalidNutritionEntryException("A quantidade é obrigatória.");
            }

            if (!Enum.IsDefined(day)) {
                throw new InvalidNutritionEntryException($"Dia '{day}' não é válido.");
            }

            if (!Enum.IsDefined(mealType)) {
                throw new InvalidNutritionEntryException($"Refeição '{mealType}' não é válida.");
            }

            var item = new NutritionPlanItem {
                Id        = Guid.NewGuid(),
                UserId    = userId,
                DayOfWeek = day,
                MealType  = mealType,
                ItemName  = normalizedItemName,
                Quantity  = normalizedQuantity
            };

            await _planRepository.AddAsync(item, cancellationToken);
            return item;
        }

        public async Task RemovePlanItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default) {
            var item = await _planRepository.GetByIdAsync(itemId, cancellationToken);

            // trata item inexistente ou de outro usuário como não encontrado
            if (item is null || item.UserId != userId) {
                throw new NotFoundException($"Item de plano '{itemId}' não encontrado.");
            }

            await _planRepository.DeleteAsync(item, cancellationToken);
        }

        // monta as quatro refeições na ordem do enum, mantendo grupos vazios quando necessário
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
