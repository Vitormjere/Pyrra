using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Focos;

namespace Pyrra.Application.Focos {
    public class DailyScoreCalculator : IDailyScoreCalculator {
        // Fração mínima do peso do dia para considerar a meta batida. Fixo por enquanto;
        // vira configuração por usuário quando o app tiver metas personalizadas.
        private const decimal GoalThreshold = 0.70m;

        private readonly IDailyFocusRepository _focusRepository;
        private readonly IFocusLogRepository   _logRepository;

        public DailyScoreCalculator(IDailyFocusRepository focusRepository, IFocusLogRepository logRepository) {
            _focusRepository = focusRepository;
            _logRepository   = logRepository;
        }

        // Estado atual: focos ativos de agora cruzados com os logs do dia. Não persiste nada.
        public async Task<DailyScoreResult> CalculateLiveAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default) {
            var focuses = await _focusRepository.GetAllByUserIdAsync(userId, cancellationToken);
            var activeFocuses = focuses.Where(f => f.Active).ToList();

            var focusIds = activeFocuses.Select(f => f.Id).ToList();
            var logs = await _logRepository.GetByFocusIdsAndDateAsync(focusIds, date, cancellationToken);
            var logsByFocus = logs.ToDictionary(l => l.DailyFocusId);

            return new DailyScoreResult(
                CalculateScore(userId, date, activeFocuses, logsByFocus),
                BuildStatuses(activeFocuses, logsByFocus));
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

        // Um foco ativo sem log naquela data não tem entrada no mapa -> Completed = false e peso atual.
        private static IReadOnlyList<FocusStatus> BuildStatuses(IReadOnlyList<DailyFocus> activeFocuses, IReadOnlyDictionary<Guid, FocusLog> logsByFocus) =>
            activeFocuses
                .Select(f => {
                    logsByFocus.TryGetValue(f.Id, out var log);
                    return new FocusStatus(f.Id, f.Name, EffectiveWeight(f, log), log is { Completed: true });
                })
                .ToList();
    }
}
