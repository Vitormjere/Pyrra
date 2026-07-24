using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Common;
using Pyrra.Domain.Treinos;

namespace Pyrra.Application.Treinos {
    public class WorkoutService : IWorkoutService {
        private readonly IWorkoutLogRepository          _repository;
        private readonly IWorkoutPlanDayRepository      _planRepository;
        private readonly IWorkoutPlanExerciseRepository _planExerciseRepository;
        private readonly IUserRepository                _userRepository;
        private readonly IClockService                  _clock;

        public WorkoutService(
            IWorkoutLogRepository          repository,
            IWorkoutPlanDayRepository      planRepository,
            IWorkoutPlanExerciseRepository planExerciseRepository,
            IUserRepository                userRepository,
            IClockService                  clock) {
            _repository             = repository;
            _planRepository         = planRepository;
            _planExerciseRepository = planExerciseRepository;
            _userRepository         = userRepository;
            _clock                  = clock;
        }

        public async Task<WorkoutLog> CreateAsync(Guid userId, CreateWorkoutInput input, CancellationToken cancellationToken = default) {
            var date = await ResolveDateAsync(userId, input.Date, cancellationToken);

            var log = new WorkoutLog {
                Id        = Guid.NewGuid(),
                UserId    = userId,
                CreatedAt = _clock.UtcNow
            };

            PopulateLog(log, input, date);

            await _repository.AddAsync(log, cancellationToken);
            return log;
        }

        public async Task<WorkoutLog> UpdateAsync(Guid userId, Guid workoutId, CreateWorkoutInput input, CancellationToken cancellationToken = default) {
            var log  = await GetOwnedLogAsync(userId, workoutId, cancellationToken);
            var date = await ResolveDateAsync(userId, input.Date, cancellationToken);

            // Mesma validação por tipo do Create. PopulateLog zera os campos da outra modalidade
            // antes de aplicar, então trocar Academia↔Corrida na edição não deixa resíduo.
            PopulateLog(log, input, date);

            await _repository.UpdateAsync(log, cancellationToken);
            return log;
        }

        public async Task DeleteAsync(Guid userId, Guid workoutId, CancellationToken cancellationToken = default) {
            var log = await GetOwnedLogAsync(userId, workoutId, cancellationToken);
            await _repository.DeleteAsync(log, cancellationToken);
        }

        // Aplica o input ao log, único ponto de validação por tipo. Zera TODOS os campos de
        // modalidade antes de aplicar: numa edição que muda o tipo, os campos da modalidade
        // antiga precisam sumir, e num log novo isso é só um no-op sobre nulos.
        private static void PopulateLog(WorkoutLog log, CreateWorkoutInput input, DateOnly date) {
            log.Type  = input.Type;
            log.Date  = date;
            log.Notes = Normalize(input.Notes);

            log.ExerciseName    = null;
            log.LoadKg          = null;
            log.Sets            = null;
            log.Reps            = null;
            log.DistanceKm      = null;
            log.DurationMinutes = null;
            log.PaceMinPerKm    = null;

            switch (input.Type) {
                case WorkoutType.Academia:
                    ApplyAcademia(log, input);
                    break;
                case WorkoutType.Corrida:
                    ApplyCorrida(log, input);
                    break;
                default:
                    throw new InvalidWorkoutException($"Tipo de treino '{input.Type}' não é suportado.");
            }
        }

        // Mesmo NotFoundException genérico dos outros módulos: inexistente ou de outro usuário
        // são indistinguíveis para quem chama.
        private async Task<WorkoutLog> GetOwnedLogAsync(Guid userId, Guid workoutId, CancellationToken cancellationToken) {
            var log = await _repository.GetByIdAsync(workoutId, cancellationToken);
            if (log is null || log.UserId != userId) {
                throw new NotFoundException($"Treino '{workoutId}' não encontrado.");
            }
            return log;
        }

        public Task<IReadOnlyList<WorkoutLog>> GetAllForUserAsync(Guid userId, WorkoutType? type = null, CancellationToken cancellationToken = default) =>
            _repository.GetAllByUserIdAsync(userId, type, cancellationToken);

        public async Task<IReadOnlyList<WorkoutLog>> GetForRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) {
            // Intervalo invertido devolve vazio, mesmo critério de tarefas e finanças.
            if (startDate > endDate) {
                return Array.Empty<WorkoutLog>();
            }

            return await _repository.GetByUserAndDateRangeAsync(userId, startDate, endDate, cancellationToken);
        }

        public async Task<IReadOnlyList<WorkoutPlanDay>> GetPlanAsync(Guid userId, CancellationToken cancellationToken = default) {
            var saved = await _planRepository.GetByUserAsync(userId, cancellationToken);
            return BuildFullWeek(userId, saved);
        }

        public async Task<IReadOnlyList<WorkoutPlanDay>> SavePlanAsync(Guid userId, IReadOnlyList<WorkoutPlanDay> days, CancellationToken cancellationToken = default) {
            // Normaliza aqui, na entrada: label só de espaços é "sem plano", e gravar "  "
            // faria o dia parecer preenchido para quem lê depois.
            var normalized = days
                .Select(day => new WorkoutPlanDay {
                    DayOfWeek = day.DayOfWeek,
                    Label     = string.IsNullOrWhiteSpace(day.Label) ? null : day.Label.Trim()
                })
                .ToList();

            await _planRepository.UpsertManyAsync(userId, normalized, cancellationToken);
            return await GetPlanAsync(userId, cancellationToken);
        }

        public async Task<IReadOnlyList<WorkoutPlanDayWithExercises>> GetPlanWithExercisesAsync(Guid userId, CancellationToken cancellationToken = default) {
            var days = await GetPlanAsync(userId, cancellationToken);

            // Uma consulta para a semana inteira, agrupada em memória: sete consultas
            // dia a dia buscariam os mesmos dados em sete idas ao banco.
            var exercises = await _planExerciseRepository.GetByUserAsync(userId, cancellationToken);
            var byDay = exercises.GroupBy(e => e.DayOfWeek).ToDictionary(g => g.Key, g => g.ToList());

            return days
                .Select(day => new WorkoutPlanDayWithExercises(
                    day.DayOfWeek,
                    day.Label,
                    byDay.TryGetValue(day.DayOfWeek, out var list)
                        ? list
                        : (IReadOnlyList<WorkoutPlanExercise>)Array.Empty<WorkoutPlanExercise>()))
                .ToList();
        }

        public async Task<WorkoutPlanExercise> AddPlanExerciseAsync(Guid userId, WeekDay dayOfWeek, WorkoutType type, string exerciseName, int? sets, int? reps, CancellationToken cancellationToken = default) {
            var normalizedName = exerciseName?.Trim();
            if (string.IsNullOrEmpty(normalizedName)) {
                throw new InvalidWorkoutException("Informe o nome do exercício.");
            }

            if (!Enum.IsDefined(dayOfWeek)) {
                throw new InvalidWorkoutException($"Dia '{dayOfWeek}' não é válido.");
            }

            if (!Enum.IsDefined(type)) {
                throw new InvalidWorkoutException($"Tipo de treino '{type}' não é válido.");
            }

            if (sets is <= 0) {
                throw new InvalidWorkoutException("O número de séries deve ser maior que zero.");
            }

            if (reps is <= 0) {
                throw new InvalidWorkoutException("O número de repetições deve ser maior que zero.");
            }

            var existing = await _planExerciseRepository.GetByUserAndDayAsync(userId, dayOfWeek, cancellationToken);

            // Próximo da lista = maior Order + 1, e não Count: remover um item do meio
            // deixaria Count menor que o último Order, e o novo colidiria com um existente.
            var nextOrder = existing.Count == 0 ? 0 : existing.Max(e => e.Order) + 1;

            var isGym = type == WorkoutType.Academia;

            var exercise = new WorkoutPlanExercise {
                Id           = Guid.NewGuid(),
                UserId       = userId,
                DayOfWeek    = dayOfWeek,
                Type         = type,
                ExerciseName = normalizedName,
                // Descartados em Corrida em vez de recusados: o cliente pode ter deixado
                // valores no formulário ao trocar de modalidade, e barrar por isso seria
                // fricção sem ganho — o dado simplesmente não se aplica.
                Sets         = isGym ? sets : null,
                Reps         = isGym ? reps : null,
                Order        = nextOrder
            };

            await _planExerciseRepository.AddAsync(exercise, cancellationToken);
            return exercise;
        }

        public async Task RemovePlanExerciseAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken = default) {
            var exercise = await _planExerciseRepository.GetByIdAsync(exerciseId, cancellationToken);

            // Mesmo NotFoundException genérico dos outros módulos: inexistente ou de outro
            // usuário são indistinguíveis para quem chama.
            if (exercise is null || exercise.UserId != userId) {
                throw new NotFoundException($"Exercício de plano '{exerciseId}' não encontrado.");
            }

            // Sem renumerar os Order dos seguintes: buracos na sequência não afetam a
            // ordenação, e reescrever a lista inteira a cada remoção seria custo sem ganho.
            await _planExerciseRepository.DeleteAsync(exercise, cancellationToken);
        }

        // Completa a semana com os dias que nunca foram salvos. Devolver os 7 sempre poupa o
        // cliente de saber quais existem no banco — para ele, todo dia tem um plano, que pode
        // estar vazio.
        private static IReadOnlyList<WorkoutPlanDay> BuildFullWeek(Guid userId, IReadOnlyList<WorkoutPlanDay> saved) {
            var byDay = saved.ToDictionary(d => d.DayOfWeek);

            return Enum.GetValues<WeekDay>()
                .Select(day =>
                    byDay.TryGetValue(day, out var existing)
                        ? existing
                        : new WorkoutPlanDay { Id = Guid.Empty, UserId = userId, DayOfWeek = day, Label = null })
                .ToList();
        }

        // Nome em branco listaria todos os treinos de Academia como se fossem um exercício só,
        // o que não é histórico de nada.
        public Task<IReadOnlyList<WorkoutLog>> GetHistoryByExerciseAsync(Guid userId, string exerciseName, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(exerciseName)) {
                throw new InvalidWorkoutException("Informe o nome do exercício para consultar o histórico.");
            }

            return _repository.GetByExerciseNameAsync(userId, exerciseName.Trim(), cancellationToken);
        }

        // Academia: nome do exercício e carga são obrigatórios; séries e repetições ficam opcionais
        // (nem todo registro guarda esse detalhe). Carga 0 é válida — exercício com peso do corpo.
        //
        // Campos de Corrida que venham preenchidos são ignorados: não são lidos daqui e, como o
        // log só recebe o que é atribuído abaixo, chegam nulos ao banco.
        private static void ApplyAcademia(WorkoutLog log, CreateWorkoutInput input) {
            var exerciseName = Normalize(input.ExerciseName);
            if (exerciseName is null) {
                throw new InvalidWorkoutException("Treino de Academia exige o nome do exercício.");
            }

            if (input.LoadKg is null) {
                throw new InvalidWorkoutException("Treino de Academia exige a carga em kg.");
            }

            if (input.LoadKg < 0) {
                throw new InvalidWorkoutException("A carga em kg não pode ser negativa.");
            }

            if (input.Sets is <= 0) {
                throw new InvalidWorkoutException("O número de séries deve ser maior que zero.");
            }

            if (input.Reps is <= 0) {
                throw new InvalidWorkoutException("O número de repetições deve ser maior que zero.");
            }

            log.ExerciseName = exerciseName;
            log.LoadKg       = input.LoadKg;
            log.Sets         = input.Sets;
            log.Reps         = input.Reps;
        }

        // Corrida: distância e duração são obrigatórias. O pace é derivado delas quando não vem
        // informado, para o histórico nunca ter registro sem pace.
        //
        // Campos de Academia que venham preenchidos são ignorados, mesmo critério do ApplyAcademia.
        private static void ApplyCorrida(WorkoutLog log, CreateWorkoutInput input) {
            if (input.DistanceKm is null) {
                throw new InvalidWorkoutException("Treino de Corrida exige a distância em km.");
            }

            if (input.DurationMinutes is null) {
                throw new InvalidWorkoutException("Treino de Corrida exige a duração em minutos.");
            }

            if (input.DistanceKm <= 0) {
                throw new InvalidWorkoutException("A distância em km deve ser maior que zero.");
            }

            if (input.DurationMinutes <= 0) {
                throw new InvalidWorkoutException("A duração em minutos deve ser maior que zero.");
            }

            if (input.PaceMinPerKm is <= 0) {
                throw new InvalidWorkoutException("O pace deve ser maior que zero.");
            }

            log.DistanceKm      = input.DistanceKm;
            log.DurationMinutes = input.DurationMinutes;
            // Arredonda em 3 casas para casar com a precisão da coluna (decimal(5,3)).
            log.PaceMinPerKm    = input.PaceMinPerKm
                ?? Math.Round(input.DurationMinutes.Value / input.DistanceKm.Value, 3, MidpointRounding.AwayFromZero);
        }

        // Treino é registrado depois de feito — muitas vezes à noite, ou dias depois — então
        // qualquer data passada vale, ao contrário do check-in de foco. O que não faz sentido é
        // registrar um treino que ainda não aconteceu, e "futuro" é no fuso do usuário: 22h em
        // São Paulo já é o dia seguinte em UTC.
        private async Task<DateOnly> ResolveDateAsync(Guid userId, DateOnly? date, CancellationToken cancellationToken) {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }

            var today      = _clock.TodayIn(user.Timezone);
            var targetDate = date ?? today;

            if (targetDate > today) {
                throw new FutureDateException(targetDate);
            }

            return targetDate;
        }

        private static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
