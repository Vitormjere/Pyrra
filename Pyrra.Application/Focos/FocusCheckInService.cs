using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Focos;

namespace Pyrra.Application.Focos {
    public class FocusCheckInService : IFocusCheckInService {
        // Fração mínima do peso do dia para considerar a meta batida. Fixo por enquanto;
        // vira configuração por usuário quando o app tiver metas personalizadas.
        private const decimal GoalThreshold = 0.70m;

        private readonly IDailyFocusRepository _focusRepository;
        private readonly IFocusLogRepository   _logRepository;
        private readonly IDailyScoreRepository _scoreRepository;
        private readonly IUserRepository       _userRepository;
        private readonly IClockService         _clock;

        public FocusCheckInService(
            IDailyFocusRepository focusRepository,
            IFocusLogRepository   logRepository,
            IDailyScoreRepository scoreRepository,
            IUserRepository       userRepository,
            IClockService         clock) {
            _focusRepository = focusRepository;
            _logRepository   = logRepository;
            _scoreRepository = scoreRepository;
            _userRepository  = userRepository;
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

            return await RecalculateScoreAsync(userId, targetDate, cancellationToken);
        }

        public async Task<DailyScoreResult> GetDailyScoreAsync(Guid userId, DateOnly? date, CancellationToken cancellationToken = default) {
            var (targetDate, today) = await ResolveDateAsync(userId, date, cancellationToken);

            // Dia passado é história: devolve o que foi gravado na época, sem reprocessar com
            // os focos/pesos de agora. Só o dia corrente reflete o estado atual ao vivo.
            return targetDate < today
                ? await GetHistoricalScoreAsync(userId, targetDate, cancellationToken)
                : await GetLiveScoreAsync(userId, targetDate, cancellationToken);
        }

        // Estado atual: focos ativos de agora cruzados com os logs do dia. Não persiste nada.
        private async Task<DailyScoreResult> GetLiveScoreAsync(Guid userId, DateOnly date, CancellationToken cancellationToken) {
            var activeFocuses = await GetActiveFocusesAsync(userId, cancellationToken);
            var logsByFocus   = await GetLogsByFocusAsync(activeFocuses, date, cancellationToken);

            return new DailyScoreResult(
                CalculateScore(userId, date, activeFocuses, logsByFocus),
                BuildStatuses(activeFocuses, logsByFocus));
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
        // o score converge para o estado real mesmo se um log for alterado por fora.
        private async Task<DailyScoreResult> RecalculateScoreAsync(Guid userId, DateOnly date, CancellationToken cancellationToken) {
            var activeFocuses = await GetActiveFocusesAsync(userId, cancellationToken);
            var logsByFocus   = await GetLogsByFocusAsync(activeFocuses, date, cancellationToken);

            var score = CalculateScore(userId, date, activeFocuses, logsByFocus);

            var saved = await _scoreRepository.UpsertAsync(score, cancellationToken);
            return new DailyScoreResult(saved, BuildStatuses(activeFocuses, logsByFocus));
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

        // Peso do foco NAQUELE dia: se houve check-in, vale o peso congelado no log; senão, o peso
        // atual do foco. As duas somas do score usam esta mesma régua — usar o peso do log só em
        // PointsEarned e o atual em PointsPossible permitiria earned > possible (percentual > 100%).
        private static int EffectiveWeight(DailyFocus focus, FocusLog? log) =>
            log?.WeightAtTimeOfLog ?? focus.Weight;

        // Única fonte da regra de pontuação: consulta e check-in passam por aqui, então não há
        // como os dois caminhos divergirem. Puro — não toca em repositório.
        private static DailyScore CalculateScore(Guid userId, DateOnly date, IReadOnlyList<DailyFocus> activeFocuses, IReadOnlyDictionary<Guid, FocusLog> logsByFocus) {
            var pointsPossible = 0;
            var pointsEarned   = 0;

            foreach (var focus in activeFocuses) {
                logsByFocus.TryGetValue(focus.Id, out var log);
                var weight = EffectiveWeight(focus, log);

                pointsPossible += weight;
                if (log is { Completed: true }) {
                    pointsEarned += weight;
                }
            }

            // Sem focos ativos não há divisão possível: o dia vale 0% em vez de estourar.
            var percentage = pointsPossible == 0
                ? 0m
                : (decimal)pointsEarned / pointsPossible;

            return new DailyScore {
                UserId         = userId,
                Date           = date,
                PointsEarned   = pointsEarned,
                PointsPossible = pointsPossible,
                Percentage     = decimal.Round(percentage, 4),
                GoalMet        = pointsPossible > 0 && percentage >= GoalThreshold
            };
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

        private async Task<IReadOnlyList<DailyFocus>> GetActiveFocusesAsync(Guid userId, CancellationToken cancellationToken) {
            var focuses = await _focusRepository.GetAllByUserIdAsync(userId, cancellationToken);
            return focuses.Where(f => f.Active).ToList();
        }

        private async Task<IReadOnlyDictionary<Guid, FocusLog>> GetLogsByFocusAsync(IReadOnlyList<DailyFocus> focuses, DateOnly date, CancellationToken cancellationToken) {
            var focusIds = focuses.Select(f => f.Id).ToList();
            var logs = await _logRepository.GetByFocusIdsAndDateAsync(focusIds, date, cancellationToken);
            return logs.ToDictionary(l => l.DailyFocusId);
        }

        // Um foco ativo sem log naquela data não tem entrada no mapa -> Completed = false e peso atual.
        private static IReadOnlyList<FocusStatus> BuildStatuses(IReadOnlyList<DailyFocus> activeFocuses, IReadOnlyDictionary<Guid, FocusLog> logsByFocus) =>
            activeFocuses
                .Select(f => {
                    logsByFocus.TryGetValue(f.Id, out var log);
                    return new FocusStatus(f.Id, f.Name, EffectiveWeight(f, log), log is { Completed: true });
                })
                .ToList();

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
