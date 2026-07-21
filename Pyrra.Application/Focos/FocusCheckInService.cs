using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Application.Streaks;
using Pyrra.Domain.Focos;

namespace Pyrra.Application.Focos {
    public class FocusCheckInService : IFocusCheckInService {
        private readonly IDailyFocusRepository _focusRepository;
        private readonly IFocusLogRepository   _logRepository;
        private readonly IDailyScoreRepository _scoreRepository;
        private readonly IUserRepository       _userRepository;
        private readonly IDailyScoreCalculator _calculator;
        private readonly IStreakService        _streakService;
        private readonly IClockService         _clock;

        public FocusCheckInService(
            IDailyFocusRepository focusRepository,
            IFocusLogRepository   logRepository,
            IDailyScoreRepository scoreRepository,
            IUserRepository       userRepository,
            IDailyScoreCalculator calculator,
            IStreakService        streakService,
            IClockService         clock) {
            _focusRepository = focusRepository;
            _logRepository   = logRepository;
            _scoreRepository = scoreRepository;
            _userRepository  = userRepository;
            _calculator      = calculator;
            _streakService   = streakService;
            _clock           = clock;
        }

        public async Task<DailyScoreResult> ToggleCheckInAsync(Guid userId, Guid focusId, DateOnly? date, CancellationToken cancellationToken = default) {
            var (targetDate, today) = await ResolveDateAsync(userId, date, cancellationToken);

            // Só o dia corrente aceita check-in. Consulta a dias passados continua liberada — o que
            // se bloqueia aqui é ESCREVER no passado, que reescreveria o DailyScore já consolidado
            // usando os focos e pesos de hoje.
            if (targetDate < today) {
                throw new PastCheckInDateException(targetDate);
            }

            var focus = await GetOwnedFocusAsync(userId, focusId, cancellationToken);

            var log = await _logRepository.GetByFocusAndDateAsync(focusId, targetDate, cancellationToken);

            if (log is null) {
                // Primeiro check-in desse foco no dia: nasce já marcado como concluído, com o
                // peso do foco congelado no momento do check-in.
                log = new FocusLog {
                    Id                = Guid.NewGuid(),
                    DailyFocusId      = focusId,
                    Date              = targetDate,
                    Completed         = true,
                    CompletedAt       = _clock.UtcNow,
                    WeightAtTimeOfLog = focus.Weight
                };
                await _logRepository.AddAsync(log, cancellationToken);
            } else {
                log.Completed   = !log.Completed;
                log.CompletedAt = log.Completed ? _clock.UtcNow : null;
                await _logRepository.UpdateAsync(log, cancellationToken);
            }

            var result = await RecalculateScoreAsync(userId, targetDate, cancellationToken);

            // Acerta dias pendentes após qualquer interação, para o streak não depender de o
            // usuário abrir a tela de streak. Só mexe em dias passados — o check-in de hoje ainda
            // não entra na contagem.
            await _streakService.SettleStreakAsync(userId, cancellationToken);

            return result;
        }

        public async Task<DailyScoreResult> GetDailyScoreAsync(Guid userId, DateOnly? date, CancellationToken cancellationToken = default) {
            var (targetDate, today) = await ResolveDateAsync(userId, date, cancellationToken);

            // Dia passado é história: devolve o que foi gravado na época, sem reprocessar com
            // os focos/pesos de agora. Só o dia corrente reflete o estado atual ao vivo.
            return targetDate < today
                ? await GetHistoricalScoreAsync(userId, targetDate, cancellationToken)
                : await _calculator.CalculateLiveAsync(userId, targetDate, cancellationToken);
        }

        // Histórico: agregado vem do DailyScore salvo e a lista é remontada a partir dos FocusLog
        // daquele dia — inclusive de focos hoje desativados, que existiam quando o dia aconteceu.
        private async Task<DailyScoreResult> GetHistoricalScoreAsync(Guid userId, DateOnly date, CancellationToken cancellationToken) {
            var stored = await _scoreRepository.GetByUserAndDateAsync(userId, date, cancellationToken);

            // Nunca houve check-in nesse dia: não há histórico a mostrar, e não é o caso de
            // inventar um a partir dos focos de hoje.
            if (stored is null) {
                return new DailyScoreResult(EmptyScore(userId, date), Array.Empty<FocusStatus>());
            }

            var allFocuses = await _focusRepository.GetAllByUserIdAsync(userId, cancellationToken);
            var focusById  = allFocuses.ToDictionary(f => f.Id);

            var logs = await _logRepository.GetByFocusIdsAndDateAsync(
                allFocuses.Select(f => f.Id).ToList(), date, cancellationToken);

            var statuses = logs
                .Where(l => focusById.ContainsKey(l.DailyFocusId))
                .Select(l => {
                    var focus = focusById[l.DailyFocusId];
                    // Peso congelado no log: editar o peso do foco hoje não reescreve o passado.
                    return new FocusStatus(focus.Id, focus.Name, l.WeightAtTimeOfLog, l.Completed);
                })
                .OrderBy(s => s.Name)
                .ToList();

            return new DailyScoreResult(stored, statuses);
        }

        // Recalcula o dia inteiro a partir dos logs, em vez de somar/subtrair o foco alternado:
        // o score converge para o estado real mesmo se um log for alterado por fora. Único caminho
        // que persiste DailyScore.
        private async Task<DailyScoreResult> RecalculateScoreAsync(Guid userId, DateOnly date, CancellationToken cancellationToken) {
            var live  = await _calculator.CalculateLiveAsync(userId, date, cancellationToken);
            var saved = await _scoreRepository.UpsertAsync(live.Score, cancellationToken);
            return new DailyScoreResult(saved, live.Focuses);
        }

        // Resolve a data alvo no fuso do usuário e barra datas futuras. Devolve também o "hoje"
        // para quem precisa comparar passado/presente sem consultar o usuário de novo.
        private async Task<(DateOnly TargetDate, DateOnly Today)> ResolveDateAsync(Guid userId, DateOnly? date, CancellationToken cancellationToken) {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }

            var today      = _clock.TodayIn(user.Timezone);
            var targetDate = date ?? today;

            if (targetDate > today) {
                throw new FutureDateException(targetDate);
            }

            return (targetDate, today);
        }

        private static DailyScore EmptyScore(Guid userId, DateOnly date) =>
            new() {
                Id             = Guid.Empty,
                UserId         = userId,
                Date           = date,
                PointsEarned   = 0,
                PointsPossible = 0,
                Percentage     = 0m,
                GoalMet        = false
            };

        // Mesmo NotFoundException genérico usado no DailyFocusService: foco inexistente,
        // de outro usuário ou desativado são indistinguíveis para quem chama.
        private async Task<DailyFocus> GetOwnedFocusAsync(Guid userId, Guid focusId, CancellationToken cancellationToken) {
            var focus = await _focusRepository.GetByIdAsync(focusId, cancellationToken);
            if (focus is null || focus.UserId != userId || !focus.Active) {
                throw new NotFoundException($"Foco '{focusId}' não encontrado.");
            }
            return focus;
        }
    }
}
