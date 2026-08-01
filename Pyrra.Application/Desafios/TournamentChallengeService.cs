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
            IUserRepository                  userRepository,
            IClockService                    clock) {
            _tournamentRepository          = tournamentRepository;
            _challengeRepository           = challengeRepository;
            _categoryRepository            = categoryRepository;
            _tournamentChallengeRepository = tournamentChallengeRepository;
            _ownChallengeRepository        = ownChallengeRepository;
            _submissionRepository          = submissionRepository;
            _teamRepository                = teamRepository;
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
            var linkedIds = (await _tournamentChallengeRepository.GetByTournamentAsync(tournamentId, cancellationToken))
                .Select(l => l.ChallengeId)
                .ToHashSet();

            var result = new List<TournamentCatalogChallengeStatus>(challenges.Count);
            foreach (var challenge in challenges) {
                // ignora desafios cuja categoria sumiu do catálogo
                if (!categoriesById.TryGetValue(challenge.CategoryId, out var category)) {
                    continue;
                }
                result.Add(new TournamentCatalogChallengeStatus(challenge, category, linkedIds.Contains(challenge.Id)));
            }

            return result
                .OrderBy(s => s.Category.Name)
                .ThenBy(s => s.Challenge.Title)
                .ToList();
        }

        public async Task LinkCatalogChallengeAsync(Guid ownerId, Guid tournamentId, Guid challengeId, CancellationToken cancellationToken = default) {
            await EnsureOwnerAsync(ownerId, tournamentId, cancellationToken);

            var challenge = await _challengeRepository.GetByIdAsync(challengeId, cancellationToken);
            if (challenge is null) {
                throw new NotFoundException($"Desafio '{challengeId}' não encontrado.");
            }

            // evita duplicar vínculo em chamadas repetidas
            var existing = await _tournamentChallengeRepository.GetAsync(tournamentId, challengeId, cancellationToken);
            if (existing is not null) {
                return;
            }

            await _tournamentChallengeRepository.AddAsync(new TournamentChallenge {
                Id           = Guid.NewGuid(),
                TournamentId = tournamentId,
                ChallengeId  = challengeId,
                LinkedAt     = _clock.UtcNow
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
            Guid ownerId, Guid tournamentId, string title, string? description, int points, CancellationToken cancellationToken = default) {
            await EnsureOwnerAsync(ownerId, tournamentId, cancellationToken);

            var normalizedTitle = ValidateInput(title, points);
            var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

            var now = _clock.UtcNow;
            var challenge = new TournamentOwnChallenge {
                Id           = Guid.NewGuid(),
                TournamentId = tournamentId,
                Title        = normalizedTitle,
                Description  = normalizedDescription,
                Points       = points,
                CreatedAt    = now,
                UpdatedAt    = now
            };

            await _ownChallengeRepository.AddAsync(challenge, cancellationToken);
            return challenge;
        }

        public async Task<TournamentOwnChallenge> UpdateOwnChallengeAsync(
            Guid ownerId, Guid tournamentId, Guid challengeId, string title, string? description, int points, CancellationToken cancellationToken = default) {
            await EnsureOwnerAsync(ownerId, tournamentId, cancellationToken);

            var challenge = await GetOwnChallengeForTournamentAsync(tournamentId, challengeId, cancellationToken);

            var normalizedTitle = ValidateInput(title, points);
            var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

            challenge.Title       = normalizedTitle;
            challenge.Description = normalizedDescription;
            challenge.Points      = points;
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

            // catálogo pequeno de times por torneio, busca individual em vez de bulk (mesmo
            // critério de outros pontos do projeto que evitam adicionar um GetByIdsAsync só pra
            // uma coleção que na prática é pequena)
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
                        ToSummary(submitter), submission.TeamId, teamName));
                } else if (submission.Source == ChallengeSource.TorneioProprio) {
                    if (!ownById.TryGetValue(submission.ChallengeId, out var ownChallenge)) {
                        continue;
                    }
                    result.Add(new PendingTournamentSubmissionWithTeam(
                        submission, ownChallenge.Title, ownChallenge.Points, submission.Source,
                        ToSummary(submitter), submission.TeamId, teamName));
                }
            }

            return result.OrderBy(p => p.Submission.CreatedAt).ToList();
        }

        private async Task<Dictionary<Guid, User>> LoadUsersAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken) {
            var distinct = ids.Distinct().ToList();
            var users    = await _userRepository.GetByIdsAsync(distinct, cancellationToken);
            return users.ToDictionary(u => u.Id);
        }

        private static UserSummary ToSummary(User user) => new(user.Id, user.Name, user.Username);

        // Garante que o usuário é dono do torneio. Quem não for recebe NotFound genérico — mesmo
        // critério de TournamentService.GetOwnedTournamentAsync.
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
    }
}
