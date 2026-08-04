using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Application.Comunidade;
using Pyrra.Domain.Desafios;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Desafios {
    public class TournamentChallengeService : ITournamentChallengeService {
        private readonly ITournamentRepository            _tournamentRepository;
        private readonly IChallengeRepository              _challengeRepository;
        private readonly IChallengeCategoryRepository      _categoryRepository;
        private readonly ITournamentChallengeRepository    _tournamentChallengeRepository;
        private readonly ITournamentOwnChallengeRepository _ownChallengeRepository;
        private readonly IChallengeSubmissionRepository    _submissionRepository;
        private readonly ITeamRepository                   _teamRepository;
        private readonly ITournamentTeamRepository          _tournamentTeamRepository;
        private readonly IUserRepository                   _userRepository;
        private readonly IClockService                     _clock;

        public TournamentChallengeService(
            ITournamentRepository            tournamentRepository,
            IChallengeRepository             challengeRepository,
            IChallengeCategoryRepository     categoryRepository,
            ITournamentChallengeRepository   tournamentChallengeRepository,
            ITournamentOwnChallengeRepository ownChallengeRepository,
            IChallengeSubmissionRepository   submissionRepository,
            ITeamRepository                  teamRepository,
            ITournamentTeamRepository        tournamentTeamRepository,
            IUserRepository                  userRepository,
            IClockService                    clock) {
            _tournamentRepository          = tournamentRepository;
            _challengeRepository           = challengeRepository;
            _categoryRepository            = categoryRepository;
            _tournamentChallengeRepository = tournamentChallengeRepository;
            _ownChallengeRepository        = ownChallengeRepository;
            _submissionRepository          = submissionRepository;
            _teamRepository                = teamRepository;
            _tournamentTeamRepository      = tournamentTeamRepository;
            _userRepository                = userRepository;
            _clock                         = clock;
        }

        public async Task<IReadOnlyList<TournamentCatalogChallengeStatus>> GetCatalogAsync(Guid ownerId, Guid tournamentId, CancellationToken cancellationToken = default) {
            await EnsureOwnerAsync(ownerId, tournamentId, cancellationToken);

            var challenges = await _challengeRepository.GetAllAsync(cancellationToken);
            if (challenges.Count == 0) {
                return Array.Empty<TournamentCatalogChallengeStatus>();
            }

            var categoriesById = (await _categoryRepository.GetAllAsync(cancellationToken)).ToDictionary(c => c.Id);
            var linksByChallenge = (await _tournamentChallengeRepository.GetByTournamentAsync(tournamentId, cancellationToken))
                .ToDictionary(l => l.ChallengeId);

            var result = new List<TournamentCatalogChallengeStatus>(challenges.Count);
            foreach (var challenge in challenges) {
                // ignora desafios cuja categoria sumiu do catálogo
                if (!categoriesById.TryGetValue(challenge.CategoryId, out var category)) {
                    continue;
                }

                var isLinked = linksByChallenge.TryGetValue(challenge.Id, out var link);
                result.Add(new TournamentCatalogChallengeStatus(challenge, category, isLinked, link?.Goal, link?.Unit));
            }

            return result
                .OrderBy(s => s.Category.Name)
                .ThenBy(s => s.Challenge.Title)
                .ToList();
        }

        public async Task LinkCatalogChallengeAsync(
            Guid ownerId, Guid tournamentId, Guid challengeId, decimal? goal, string? unit, CancellationToken cancellationToken = default) {
            await EnsureOwnerAsync(ownerId, tournamentId, cancellationToken);

            var challenge = await _challengeRepository.GetByIdAsync(challengeId, cancellationToken);
            if (challenge is null) {
                throw new NotFoundException($"Desafio '{challengeId}' não encontrado.");
            }

            var normalizedGoal = ValidateGoal(goal, ref unit);

            // já vinculado: atualiza a meta/unidade em vez de não fazer nada — evita ter que desvincular e vincular de novo só pra editar
            var existing = await _tournamentChallengeRepository.GetAsync(tournamentId, challengeId, cancellationToken);
            if (existing is not null) {
                existing.Goal = normalizedGoal;
                existing.Unit = unit;
                await _tournamentChallengeRepository.UpdateAsync(existing, cancellationToken);
                return;
            }

            await _tournamentChallengeRepository.AddAsync(new TournamentChallenge {
                Id           = Guid.NewGuid(),
                TournamentId = tournamentId,
                ChallengeId  = challengeId,
                LinkedAt     = _clock.UtcNow,
                Goal         = normalizedGoal,
                Unit         = unit
            }, cancellationToken);
        }

        public async Task UnlinkCatalogChallengeAsync(Guid ownerId, Guid tournamentId, Guid challengeId, CancellationToken cancellationToken = default) {
            await EnsureOwnerAsync(ownerId, tournamentId, cancellationToken);

            var existing = await _tournamentChallengeRepository.GetAsync(tournamentId, challengeId, cancellationToken);
            if (existing is null) {
                // evita erro ao desvincular algo que já não está vinculado
                return;
            }

            await _tournamentChallengeRepository.RemoveAsync(existing, cancellationToken);
        }

        public async Task<IReadOnlyList<TournamentOwnChallenge>> GetOwnChallengesAsync(Guid ownerId, Guid tournamentId, CancellationToken cancellationToken = default) {
            await EnsureOwnerAsync(ownerId, tournamentId, cancellationToken);
            return await _ownChallengeRepository.GetByTournamentAsync(tournamentId, cancellationToken);
        }

        public async Task<TournamentOwnChallenge> CreateOwnChallengeAsync(
            Guid ownerId, Guid tournamentId, string title, string? description, int points, decimal? goal, string? unit,
            CancellationToken cancellationToken = default) {
            await EnsureOwnerAsync(ownerId, tournamentId, cancellationToken);

            var normalizedTitle = ValidateInput(title, points);
            var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            var normalizedGoal = ValidateGoal(goal, ref unit);

            var now = _clock.UtcNow;
            var challenge = new TournamentOwnChallenge {
                Id           = Guid.NewGuid(),
                TournamentId = tournamentId,
                Title        = normalizedTitle,
                Description  = normalizedDescription,
                Points       = points,
                Goal         = normalizedGoal,
                Unit         = unit,
                CreatedAt    = now,
                UpdatedAt    = now
            };

            await _ownChallengeRepository.AddAsync(challenge, cancellationToken);
            return challenge;
        }

        public async Task<TournamentOwnChallenge> UpdateOwnChallengeAsync(
            Guid ownerId, Guid tournamentId, Guid challengeId, string title, string? description, int points, decimal? goal, string? unit,
            CancellationToken cancellationToken = default) {
            await EnsureOwnerAsync(ownerId, tournamentId, cancellationToken);

            var challenge = await GetOwnChallengeForTournamentAsync(tournamentId, challengeId, cancellationToken);

            var normalizedTitle = ValidateInput(title, points);
            var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            var normalizedGoal = ValidateGoal(goal, ref unit);

            challenge.Title       = normalizedTitle;
            challenge.Description = normalizedDescription;
            challenge.Points      = points;
            challenge.Goal        = normalizedGoal;
            challenge.Unit        = unit;
            challenge.UpdatedAt   = _clock.UtcNow;

            await _ownChallengeRepository.UpdateAsync(challenge, cancellationToken);
            return challenge;
        }

        public async Task DeleteOwnChallengeAsync(Guid ownerId, Guid tournamentId, Guid challengeId, CancellationToken cancellationToken = default) {
            await EnsureOwnerAsync(ownerId, tournamentId, cancellationToken);

            var challenge = await GetOwnChallengeForTournamentAsync(tournamentId, challengeId, cancellationToken);
            await _ownChallengeRepository.DeleteAsync(challenge, cancellationToken);
        }

        public async Task<IReadOnlyList<PendingTournamentSubmissionWithTeam>> GetPendingSubmissionsAsync(
            Guid ownerId, Guid tournamentId, CancellationToken cancellationToken = default) {
            await EnsureOwnerAsync(ownerId, tournamentId, cancellationToken);

            var pending = await _submissionRepository.GetPendingForTournamentAsync(tournamentId, cancellationToken);
            if (pending.Count == 0) {
                return Array.Empty<PendingTournamentSubmissionWithTeam>();
            }

            var catalogById = (await _challengeRepository.GetAllAsync(cancellationToken)).ToDictionary(c => c.Id);
            var ownById     = (await _ownChallengeRepository.GetByTournamentAsync(tournamentId, cancellationToken)).ToDictionary(c => c.Id);
            var submitters  = await LoadUsersAsync(pending.Select(s => s.UserId), cancellationToken);

            // times por torneio são poucos, então busca individual em vez de bulk — mesmo critério usado em outros pontos do projeto pra não criar um GetByIdsAsync só pra uma coleção pequena
            var teamsById = new Dictionary<Guid, string>();
            foreach (var teamId in pending.Select(s => s.TeamId).Distinct()) {
                var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken);
                if (team is not null) {
                    teamsById[teamId] = team.Name;
                }
            }

            var result = new List<PendingTournamentSubmissionWithTeam>(pending.Count);
            foreach (var submission in pending) {
                if (!submitters.TryGetValue(submission.UserId, out var submitter) ||
                    !teamsById.TryGetValue(submission.TeamId, out var teamName)) {
                    continue;
                }

                // ignora registros removidos para não quebrar a fila
                if (submission.Source == ChallengeSource.TorneioCatalogo) {
                    if (!catalogById.TryGetValue(submission.ChallengeId, out var catalogChallenge)) {
                        continue;
                    }
                    result.Add(new PendingTournamentSubmissionWithTeam(
                        submission, catalogChallenge.Title, catalogChallenge.Points, submission.Source,
                        submission.Quantity, ToSummary(submitter), submission.TeamId, teamName));
                } else if (submission.Source == ChallengeSource.TorneioProprio) {
                    if (!ownById.TryGetValue(submission.ChallengeId, out var ownChallenge)) {
                        continue;
                    }
                    result.Add(new PendingTournamentSubmissionWithTeam(
                        submission, ownChallenge.Title, ownChallenge.Points, submission.Source,
                        submission.Quantity, ToSummary(submitter), submission.TeamId, teamName));
                }
            }

            return result.OrderBy(p => p.Submission.CreatedAt).ToList();
        }

        public async Task<IReadOnlyList<TournamentChallengeProgress>> GetChallengeProgressAsync(
            Guid ownerId, Guid tournamentId, CancellationToken cancellationToken = default) {
            await EnsureOwnerAsync(ownerId, tournamentId, cancellationToken);

            var approvedEntries = await _tournamentTeamRepository.GetApprovedForTournamentAsync(tournamentId, cancellationToken);
            if (approvedEntries.Count == 0) {
                return Array.Empty<TournamentChallengeProgress>();
            }

            var teamNamesById = new Dictionary<Guid, string>();
            foreach (var entry in approvedEntries) {
                var team = await _teamRepository.GetByIdAsync(entry.TeamId, cancellationToken);
                if (team is not null) {
                    teamNamesById[entry.TeamId] = team.Name;
                }
            }

            var sumsByChallengeAndTeam = (await _submissionRepository.GetApprovedForTournamentAsync(tournamentId, cancellationToken))
                .Where(s => s.Quantity.HasValue)
                .GroupBy(s => s.ChallengeId)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(s => s.TeamId).ToDictionary(tg => tg.Key, tg => tg.Sum(s => s.Quantity!.Value)));

            IReadOnlyList<TeamChallengeProgress> BuildTeamProgress(Guid challengeId) {
                var sumsByTeam = sumsByChallengeAndTeam.GetValueOrDefault(challengeId);
                return approvedEntries
                    .Where(e => teamNamesById.ContainsKey(e.TeamId))
                    .Select(e => new TeamChallengeProgress(e.TeamId, teamNamesById[e.TeamId], sumsByTeam?.GetValueOrDefault(e.TeamId) ?? 0m))
                    .OrderByDescending(t => t.Progress)
                    .ToList();
            }

            var result = new List<TournamentChallengeProgress>();

            var linksWithGoal = (await _tournamentChallengeRepository.GetByTournamentAsync(tournamentId, cancellationToken))
                .Where(l => l.Goal is not null)
                .ToList();
            if (linksWithGoal.Count > 0) {
                var catalogById = (await _challengeRepository.GetAllAsync(cancellationToken)).ToDictionary(c => c.Id);
                foreach (var link in linksWithGoal) {
                    if (!catalogById.TryGetValue(link.ChallengeId, out var challenge)) {
                        continue;
                    }
                    result.Add(new TournamentChallengeProgress(
                        challenge.Id, challenge.Title, ChallengeSource.TorneioCatalogo, link.Goal!.Value, link.Unit!, BuildTeamProgress(challenge.Id)));
                }
            }

            var ownChallengesWithGoal = (await _ownChallengeRepository.GetByTournamentAsync(tournamentId, cancellationToken))
                .Where(c => c.Goal is not null)
                .ToList();
            foreach (var challenge in ownChallengesWithGoal) {
                result.Add(new TournamentChallengeProgress(
                    challenge.Id, challenge.Title, ChallengeSource.TorneioProprio, challenge.Goal!.Value, challenge.Unit!, BuildTeamProgress(challenge.Id)));
            }

            return result.OrderBy(p => p.ChallengeTitle).ToList();
        }

        private async Task<Dictionary<Guid, User>> LoadUsersAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken) {
            var distinct = ids.Distinct().ToList();
            var users    = await _userRepository.GetByIdsAsync(distinct, cancellationToken);
            return users.ToDictionary(u => u.Id);
        }

        private static UserSummary ToSummary(User user) => new(user.Id, user.Name, user.Username);

        private async Task EnsureOwnerAsync(Guid ownerId, Guid tournamentId, CancellationToken cancellationToken) {
            var tournament = await _tournamentRepository.GetByIdAsync(tournamentId, cancellationToken);
            if (tournament is null || tournament.OwnerId != ownerId) {
                throw new NotFoundException("Torneio não encontrado.");
            }
        }

        private async Task<TournamentOwnChallenge> GetOwnChallengeForTournamentAsync(Guid tournamentId, Guid challengeId, CancellationToken cancellationToken) {
            var challenge = await _ownChallengeRepository.GetByIdAsync(challengeId, cancellationToken);
            if (challenge is null || challenge.TournamentId != tournamentId) {
                throw new NotFoundException($"Desafio '{challengeId}' não encontrado.");
            }
            return challenge;
        }

        private static string ValidateInput(string title, int points) {
            var normalizedTitle = title?.Trim();
            if (string.IsNullOrEmpty(normalizedTitle)) {
                throw new InvalidChallengeException("O título do desafio é obrigatório.");
            }

            if (points <= 0) {
                throw new InvalidChallengeException("A pontuação do desafio precisa ser maior que zero.");
            }

            return normalizedTitle;
        }

        private static decimal? ValidateGoal(decimal? goal, ref string? unit) {
            var normalizedUnit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();

            if (goal is null && normalizedUnit is null) {
                unit = null;
                return null;
            }

            if (goal is null || normalizedUnit is null) {
                throw new InvalidChallengeException("Meta e unidade devem ser preenchidas juntas.");
            }

            if (goal <= 0) {
                throw new InvalidChallengeException("A meta precisa ser maior que zero.");
            }

            unit = normalizedUnit;
            return goal;
        }
    }
}
