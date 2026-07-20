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
        private readonly IFocusLogRepository _logRepository;
        private readonly IDailyScoreRepository _scoreRepository;

        public FocusCheckInService(
            IDailyFocusRepository focusRepository,
            IFocusLogRepository logRepository,
            IDailyScoreRepository scoreRepository) {
            _focusRepository = focusRepository;
            _logRepository   = logRepository;
            _scoreRepository = scoreRepository;
        }

        public async Task<DailyScoreResult> ToggleCheckInAsync(Guid userId, Guid focusId, DateOnly date, CancellationToken cancellationToken = default) {
            await EnsureFocusIsOwnedByUserAsync(userId, focusId, cancellationToken);

            var log = await _logRepository.GetByFocusAndDateAsync(focusId, date, cancellationToken);

            if (log is null) {
                // Primeiro check-in desse foco no dia: nasce já marcado como concluído.
                log = new FocusLog {
                    Id           = Guid.NewGuid(),
                    DailyFocusId = focusId,
                    Date         = date,
                    Completed    = true,
                    CompletedAt  = DateTime.UtcNow
                };
                await _logRepository.AddAsync(log, cancellationToken);
            } else {
                log.Completed   = !log.Completed;
                log.CompletedAt = log.Completed ? DateTime.UtcNow : null;
                await _logRepository.UpdateAsync(log, cancellationToken);
            }

            return await RecalculateScoreAsync(userId, date, cancellationToken);
        }

        // Sempre recalcula a partir do estado atual (focos ativos + logs do dia), nunca lê o
        // agregado salvo: o DailyScore só é reescrito em check-in, então desativar/reativar um
        // foco ou mudar um peso deixaria o valor salvo divergindo da lista de focos devolvida.
        // Consulta não persiste nada — quem grava é o ToggleCheckInAsync.
        public async Task<DailyScoreResult> GetDailyScoreAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default) {
            var activeFocuses = await GetActiveFocusesAsync(userId, cancellationToken);
            var completedIds  = await GetCompletedFocusIdsAsync(activeFocuses, date, cancellationToken);

            return new DailyScoreResult(
                CalculateScore(userId, date, activeFocuses, completedIds),
                BuildStatuses(activeFocuses, completedIds));
        }

        // Recalcula o dia inteiro a partir dos logs, em vez de somar/subtrair o foco alternado:
        // o score converge para o estado real mesmo se um log for alterado por fora.
        private async Task<DailyScoreResult> RecalculateScoreAsync(Guid userId, DateOnly date, CancellationToken cancellationToken) {
            var activeFocuses = await GetActiveFocusesAsync(userId, cancellationToken);
            var completedIds  = await GetCompletedFocusIdsAsync(activeFocuses, date, cancellationToken);

            var score = CalculateScore(userId, date, activeFocuses, completedIds);

            var saved = await _scoreRepository.UpsertAsync(score, cancellationToken);
            return new DailyScoreResult(saved, BuildStatuses(activeFocuses, completedIds));
        }

        // Única fonte da regra de pontuação: consulta e check-in passam por aqui, então não há
        // como os dois caminhos divergirem. Puro — não toca em repositório.
        private static DailyScore CalculateScore(Guid userId, DateOnly date, IReadOnlyList<DailyFocus> activeFocuses, HashSet<Guid> completedIds) {
            var pointsPossible = activeFocuses.Sum(f => f.Weight);
            var pointsEarned   = activeFocuses.Where(f => completedIds.Contains(f.Id)).Sum(f => f.Weight);

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

        private async Task<IReadOnlyList<DailyFocus>> GetActiveFocusesAsync(Guid userId, CancellationToken cancellationToken) {
            var focuses = await _focusRepository.GetAllByUserIdAsync(userId, cancellationToken);
            return focuses.Where(f => f.Active).ToList();
        }

        private async Task<HashSet<Guid>> GetCompletedFocusIdsAsync(IReadOnlyList<DailyFocus> activeFocuses, DateOnly date, CancellationToken cancellationToken) {
            var activeIds = activeFocuses.Select(f => f.Id).ToList();
            var logs = await _logRepository.GetByFocusIdsAndDateAsync(activeIds, date, cancellationToken);
            return logs.Where(l => l.Completed).Select(l => l.DailyFocusId).ToHashSet();
        }

        // Um foco ativo sem log naquela data simplesmente não está em completedIds -> Completed = false.
        private static IReadOnlyList<FocusStatus> BuildStatuses(IReadOnlyList<DailyFocus> activeFocuses, HashSet<Guid> completedIds) =>
            activeFocuses
                .Select(f => new FocusStatus(f.Id, f.Name, f.Weight, completedIds.Contains(f.Id)))
                .ToList();

        // Mesmo NotFoundException genérico usado no DailyFocusService: foco inexistente,
        // de outro usuário ou desativado são indistinguíveis para quem chama.
        private async Task EnsureFocusIsOwnedByUserAsync(Guid userId, Guid focusId, CancellationToken cancellationToken) {
            var focus = await _focusRepository.GetByIdAsync(focusId, cancellationToken);
            if (focus is null || focus.UserId != userId || !focus.Active) {
                throw new NotFoundException($"Foco '{focusId}' não encontrado.");
            }
        }
    }
}
