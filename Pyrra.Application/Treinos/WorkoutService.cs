using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Treinos;

namespace Pyrra.Application.Treinos {
    public class WorkoutService : IWorkoutService {
        private readonly IWorkoutLogRepository _repository;
        private readonly IUserRepository       _userRepository;
        private readonly IClockService         _clock;

        public WorkoutService(
            IWorkoutLogRepository repository,
            IUserRepository       userRepository,
            IClockService         clock) {
            _repository     = repository;
            _userRepository = userRepository;
            _clock          = clock;
        }

        public async Task<WorkoutLog> CreateAsync(Guid userId, CreateWorkoutInput input, CancellationToken cancellationToken = default) {
            var date = await ResolveDateAsync(userId, input.Date, cancellationToken);

            var log = new WorkoutLog {
                Id        = Guid.NewGuid(),
                UserId    = userId,
                Type      = input.Type,
                Date      = date,
                Notes     = Normalize(input.Notes),
                CreatedAt = _clock.UtcNow
            };

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

            await _repository.AddAsync(log, cancellationToken);
            return log;
        }

        public Task<IReadOnlyList<WorkoutLog>> GetAllForUserAsync(Guid userId, WorkoutType? type = null, CancellationToken cancellationToken = default) =>
            _repository.GetAllByUserIdAsync(userId, type, cancellationToken);

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
