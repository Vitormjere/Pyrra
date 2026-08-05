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

            // permite trocar a modalidade sem manter dados antigos
            PopulateLog(log, input, date);

            await _repository.UpdateAsync(log, cancellationToken);
            return log;
        }

        public async Task DeleteAsync(Guid userId, Guid workoutId, CancellationToken cancellationToken = default) {
            var log = await GetOwnedLogAsync(userId, workoutId, cancellationToken);
            await _repository.DeleteAsync(log, cancellationToken);
        }

        // aplica o input e limpa os dados da modalidade anterior
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

        // não diferencia treino inexistente de treino de outro usuário
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
            // intervalo inválido retorna vazio
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
            // trata label com só espaços como sem plano
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

            // busca a semana inteira em uma única consulta
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

            // usa o maior order para evitar posições repetidas
            var nextOrder = existing.Count == 0 ? 0 : existing.Max(e => e.Order) + 1;

            var isGym = type == WorkoutType.Academia;

            var exercise = new WorkoutPlanExercise {
                Id           = Guid.NewGuid(),
                UserId       = userId,
                DayOfWeek    = dayOfWeek,
                Type         = type,
                ExerciseName = normalizedName,
                // ignora sets e reps em corrida
                Sets = isGym ? sets : null,
                Reps         = isGym ? reps : null,
                Order        = nextOrder
            };

            await _planExerciseRepository.AddAsync(exercise, cancellationToken);
            return exercise;
        }

        public async Task RemovePlanExerciseAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken = default) {
            var exercise = await _planExerciseRepository.GetByIdAsync(exerciseId, cancellationToken);

            // não diferencia exercício inexistente de exercício de outro usuário
            if (exercise is null || exercise.UserId != userId) {
                throw new NotFoundException($"Exercício de plano '{exerciseId}' não encontrado.");
            }

            // mantém a ordem sem renumerar os demais
            await _planExerciseRepository.DeleteAsync(exercise, cancellationToken);
        }

        // completa a semana retornando sempre os 7 dias
        private static IReadOnlyList<WorkoutPlanDay> BuildFullWeek(Guid userId, IReadOnlyList<WorkoutPlanDay> saved) {
            var byDay = saved.ToDictionary(d => d.DayOfWeek);

            return Enum.GetValues<WeekDay>()
                .Select(day =>
                    byDay.TryGetValue(day, out var existing)
                        ? existing
                        : new WorkoutPlanDay { Id = Guid.Empty, UserId = userId, DayOfWeek = day, Label = null })
                .ToList();
        }

        // exige um nome de exercício válido
        public Task<IReadOnlyList<WorkoutLog>> GetHistoryByExerciseAsync(Guid userId, string exerciseName, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(exerciseName)) {
                throw new InvalidWorkoutException("Informe o nome do exercício para consultar o histórico.");
            }

            return _repository.GetByExerciseNameAsync(userId, exerciseName.Trim(), cancellationToken);
        }

        // em academia, nome e carga são obrigatórios
        // ignora os campos de corrida
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
            // arredonda para a precisão da coluna
            log.PaceMinPerKm    = input.PaceMinPerKm
                ?? Math.Round(input.DurationMinutes.Value / input.DistanceKm.Value, 3, MidpointRounding.AwayFromZero);
        }

        // aceita datas passadas, mas não futuras
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
