using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Desafios {
    // gera os 3 desafios do dia por time — chamado periodicamente pelo
    // DailyChallengeGeneratorHostedService (Pyrra.Api), nunca direto por um endpoint
    public class DailyChallengeGeneratorService : IDailyChallengeGeneratorService {
        private const int ChallengesPerDay = 3;

        private readonly ITeamActiveCategoryRepository _activeCategoryRepository;
        private readonly IChallengeRepository           _challengeRepository;
        private readonly ITeamDailyChallengeRepository  _dailyChallengeRepository;
        private readonly IClockService                  _clock;

        public DailyChallengeGeneratorService(
            ITeamActiveCategoryRepository activeCategoryRepository,
            IChallengeRepository          challengeRepository,
            ITeamDailyChallengeRepository dailyChallengeRepository,
            IClockService                 clock) {
            _activeCategoryRepository = activeCategoryRepository;
            _challengeRepository      = challengeRepository;
            _dailyChallengeRepository = dailyChallengeRepository;
            _clock                    = clock;
        }

        public async Task<int> GenerateMissingForTodayAsync(CancellationToken cancellationToken = default) {
            var now   = _clock.UtcNow;
            var today = DateOnly.FromDateTime(now);

            var teamsWithActiveCategories = await _activeCategoryRepository.GetDistinctTeamIdsAsync(cancellationToken);
            if (teamsWithActiveCategories.Count == 0) {
                return 0;
            }

            var teamsAlreadyGenerated = (await _dailyChallengeRepository.GetTeamIdsWithEntriesForDateAsync(today, cancellationToken))
                .ToHashSet();

            var pendingTeamIds = teamsWithActiveCategories.Where(id => !teamsAlreadyGenerated.Contains(id)).ToList();
            if (pendingTeamIds.Count == 0) {
                return 0;
            }

            var random = Random.Shared;
            var processed = 0;

            foreach (var teamId in pendingTeamIds) {
                var activeCategoryIds = (await _activeCategoryRepository.GetByTeamAsync(teamId, cancellationToken))
                    .Select(a => a.CategoryId)
                    .ToList();

                var eligible = new List<Challenge>();
                foreach (var categoryId in activeCategoryIds) {
                    var challenges = await _challengeRepository.GetByCategoryAsync(categoryId, cancellationToken);
                    // mesmo filtro de elegibilidade do catálogo antigo: fora os desafios com prazo já vencido
                    eligible.AddRange(challenges.Where(c => c.Deadline is null || c.Deadline > now));
                }

                // time com categoria ativa mas catálogo vazio (ou tudo vencido) — nada pra sortear hoje
                if (eligible.Count == 0) {
                    continue;
                }

                var picks = eligible.OrderBy(_ => random.Next()).Take(ChallengesPerDay).ToList();

                var entries = picks.Select(challenge => new TeamDailyChallenge {
                    Id          = Guid.NewGuid(),
                    TeamId      = teamId,
                    ChallengeId = challenge.Id,
                    Date        = today,
                    RevealAt    = RandomInstantWithinDay(today, random),
                    CreatedAt   = now
                }).ToList();

                await _dailyChallengeRepository.AddRangeAsync(entries, cancellationToken);
                processed++;
            }

            return processed;
        }

        // instante aleatório entre 00:00:00 e 23:59:59 (UTC) do dia — independe de quando o job
        // rodou, pra os 3 desafios ficarem espalhados pelo dia inteiro, não só a partir de agora
        private static DateTime RandomInstantWithinDay(DateOnly date, Random random) {
            var startOfDay = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
            var secondsIntoDay = random.Next(0, 24 * 60 * 60);
            return startOfDay.AddSeconds(secondsIntoDay);
        }
    }
}
