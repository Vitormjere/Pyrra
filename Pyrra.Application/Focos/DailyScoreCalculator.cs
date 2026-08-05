using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Focos;

namespace Pyrra.Application.Focos {
    public class DailyScoreCalculator : IDailyScoreCalculator {
        // define o mínimo necessário para considerar a meta concluída
        private const decimal GoalThreshold = 0.70m;

        private readonly IDailyFocusRepository _focusRepository;
        private readonly IFocusLogRepository   _logRepository;

        public DailyScoreCalculator(IDailyFocusRepository focusRepository, IFocusLogRepository logRepository) {
            _focusRepository = focusRepository;
            _logRepository   = logRepository;
        }

        // monta o estado atual cruzando focos ativos e logs do dia
        public async Task<DailyScoreResult> CalculateLiveAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default) {
            var focuses       = await _focusRepository.GetAllByUserIdAsync(userId, cancellationToken);
            var activeFocuses = focuses.Where(f => f.Active).ToList();

            var focusIds    = activeFocuses.Select(f => f.Id).ToList();
            var logs        = await _logRepository.GetByFocusIdsAndDateAsync(focusIds, date, cancellationToken);
            var logsByFocus = logs.ToDictionary(l => l.DailyFocusId);

            return new DailyScoreResult(
                CalculateScore(userId, date, activeFocuses, logsByFocus),
                BuildStatuses(activeFocuses, logsByFocus));
        }

        // usa o peso congelado do log quando houver check-in, mantendo o cálculo consistente
        private static int EffectiveWeight(DailyFocus focus, FocusLog? log) =>
            log?.WeightAtTimeOfLog ?? focus.Weight;

        // centraliza a regra de pontuação sem acessar repositórios
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

            // evita divisão sem focos ativos
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

        // sem log no dia, considera não concluído e usa o peso atual
        private static IReadOnlyList<FocusStatus> BuildStatuses(IReadOnlyList<DailyFocus> activeFocuses, IReadOnlyDictionary<Guid, FocusLog> logsByFocus) =>
            activeFocuses
                .Select(f => {
                    logsByFocus.TryGetValue(f.Id, out var log);
                    return new FocusStatus(f.Id, f.Name, EffectiveWeight(f, log), log is { Completed: true });
                })
                .ToList();
    }
}
