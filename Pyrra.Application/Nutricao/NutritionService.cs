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
            var today      = await TodayAsync(userId, cancellationToken);
            var targetDate = date ?? today;
            var entries    = await _repository.GetByUserAndDateAsync(userId, targetDate, cancellationToken);

            // Semeadura do plano: só HOJE e só se ainda não tiver ocorrido para este dia.
            //
            // O gatilho é o registro em NutritionPlanSeedLog, não a lista estar vazia. Isso
            // separa "nunca copiei" de "copiei e o usuário apagou": apagar tudo agora deixa
            // o dia vazio de verdade, sem o plano voltar na próxima carga.
            //
            // Dias passados ficam de fora porque copiar plano para trás reescreveria
            // histórico; dias futuros, porque o plano ainda pode mudar até lá.
            if (targetDate == today && !await _seedLogRepository.HasSeededAsync(userId, today, cancellationToken)) {
                var seeded = await SeedFromPlanAsync(userId, today, cancellationToken);
                if (seeded.Count > 0) {
                    // Anexa em vez de substituir: o dia pode já ter itens lançados à mão
                    // antes desta primeira leitura, e descartá-los seria perder dado real.
                    entries = entries.Concat(seeded).OrderBy(e => e.MealType).ThenBy(e => e.CreatedAt).ToList();
                }
            }

            return BuildDay(targetDate, entries);
        }

        // Copia os itens planejados do dia da semana correspondente para entries reais.
        // A partir daí eles são itens comuns: editáveis e removíveis como qualquer outro,
        // sem vínculo com o plano de origem.
        private async Task<IReadOnlyList<NutritionEntry>> SeedFromPlanAsync(Guid userId, DateOnly today, CancellationToken cancellationToken) {
            var planItems = await _planRepository.GetByUserAndDayAsync(userId, ToWeekDay(today), cancellationToken);

            // Plano vazio para este dia da semana: NÃO marca como semeado. Nada foi copiado,
            // e registrar a marca impediria a cópia caso o usuário monte o plano mais tarde
            // no mesmo dia.
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

            // Marca DEPOIS de gravar: se a cópia falhar, o dia continua não semeado e a
            // próxima carga tenta de novo, em vez de registrar uma semeadura que não houve.
            await _seedLogRepository.MarkSeededAsync(userId, today, now, cancellationToken);

            return entries;
        }

        // DateOnly.DayOfWeek começa no domingo; o WeekDay do domínio começa na segunda,
        // como o WeekRange. Este deslocamento é a única ponte entre as duas convenções.
        private static WeekDay ToWeekDay(DateOnly date) =>
            (WeekDay)(((int)date.DayOfWeek + 6) % 7);

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

        // Mesmo NotFoundException genérico dos outros módulos: item inexistente ou de outro
        // usuário são indistinguíveis para quem chama.
        private async Task<NutritionEntry> GetOwnedEntryAsync(Guid userId, Guid itemId, CancellationToken cancellationToken) {
            var entry = await _repository.GetByIdAsync(itemId, cancellationToken);
            if (entry is null || entry.UserId != userId) {
                throw new NotFoundException($"Item '{itemId}' não encontrado.");
            }
            return entry;
        }

        public async Task<IReadOnlyList<PlanDay>> GetPlanAsync(Guid userId, CancellationToken cancellationToken = default) {
            var items = await _planRepository.GetByUserAsync(userId, cancellationToken);

            // Grade completa 7x4, mesmo critério do BuildDay: refeição sem item vira lista
            // vazia, nunca grupo ausente.
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

            // Mesmo NotFoundException genérico dos outros módulos: item inexistente ou de
            // outro usuário são indistinguíveis para quem chama.
            if (item is null || item.UserId != userId) {
                throw new NotFoundException($"Item de plano '{itemId}' não encontrado.");
            }

            await _planRepository.DeleteAsync(item, cancellationToken);
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
