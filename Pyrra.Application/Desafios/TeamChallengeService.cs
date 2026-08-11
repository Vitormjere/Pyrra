using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Achievements;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Application.Comunidade;
using Pyrra.Domain.Comunidade;
using Pyrra.Domain.Desafios;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Desafios {
    public class TeamChallengeService : ITeamChallengeService {
        // usa os mesmos limites do banner de time
        private static readonly HashSet<string> AllowedSubmissionContentTypes =
            new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

        private const long MaxSubmissionImageBytes = 3 * 1024 * 1024;

        private readonly ITeamRepository                    _teamRepository;
        private readonly ITeamMemberRepository              _teamMemberRepository;
        private readonly IChallengeCategoryRepository       _categoryRepository;
        private readonly IChallengeRepository               _challengeRepository;
        private readonly ITeamActiveCategoryRepository      _activeCategoryRepository;
        private readonly ITeamDailyChallengeRepository      _dailyChallengeRepository;
        private readonly IChallengeSubmissionRepository     _submissionRepository;
        private readonly IChallengeSubmissionStorageService _submissionStorage;
        private readonly ITournamentTeamRepository          _tournamentTeamRepository;
        private readonly ITournamentRepository              _tournamentRepository;
        private readonly ITournamentChallengeRepository     _tournamentChallengeRepository;
        private readonly ITournamentOwnChallengeRepository  _tournamentOwnChallengeRepository;
        private readonly ITeamMemberScoreRepository         _memberScoreRepository;
        private readonly IUserRepository                    _userRepository;
        private readonly IClockService                      _clock;
        private readonly IAchievementCheckerService         _achievementChecker;

        public TeamChallengeService(
            ITeamRepository                    teamRepository,
            ITeamMemberRepository              teamMemberRepository,
            IChallengeCategoryRepository       categoryRepository,
            IChallengeRepository               challengeRepository,
            ITeamActiveCategoryRepository      activeCategoryRepository,
            ITeamDailyChallengeRepository      dailyChallengeRepository,
            IChallengeSubmissionRepository     submissionRepository,
            IChallengeSubmissionStorageService submissionStorage,
            ITournamentTeamRepository          tournamentTeamRepository,
            ITournamentRepository              tournamentRepository,
            ITournamentChallengeRepository     tournamentChallengeRepository,
            ITournamentOwnChallengeRepository  tournamentOwnChallengeRepository,
            ITeamMemberScoreRepository         memberScoreRepository,
            IUserRepository                    userRepository,
            IClockService                      clock,
            IAchievementCheckerService         achievementChecker) {
            _teamRepository            = teamRepository;
            _teamMemberRepository      = teamMemberRepository;
            _categoryRepository        = categoryRepository;
            _challengeRepository       = challengeRepository;
            _activeCategoryRepository  = activeCategoryRepository;
            _dailyChallengeRepository  = dailyChallengeRepository;
            _submissionRepository      = submissionRepository;
            _submissionStorage         = submissionStorage;
            _tournamentTeamRepository  = tournamentTeamRepository;
            _tournamentRepository      = tournamentRepository;
            _tournamentChallengeRepository    = tournamentChallengeRepository;
            _tournamentOwnChallengeRepository = tournamentOwnChallengeRepository;
            _memberScoreRepository     = memberScoreRepository;
            _userRepository            = userRepository;
            _clock                     = clock;
            _achievementChecker        = achievementChecker;
        }

        public async Task<IReadOnlyList<TeamCategoryStatus>> GetCategoriesForTeamAsync(Guid ownerId, Guid teamId, CancellationToken cancellationToken = default) {
            await GetOwnedTeamAsync(ownerId, teamId, cancellationToken);

            var categories = await _categoryRepository.GetAllAsync(cancellationToken);
            var activeIds  = (await _activeCategoryRepository.GetByTeamAsync(teamId, cancellationToken))
                .Select(a => a.CategoryId)
                .ToHashSet();

            return categories
                .Select(c => new TeamCategoryStatus(c, activeIds.Contains(c.Id)))
                .ToList();
        }

        public async Task ActivateCategoryAsync(Guid ownerId, Guid teamId, Guid categoryId, CancellationToken cancellationToken = default) {
            await GetOwnedTeamAsync(ownerId, teamId, cancellationToken);

            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category is null) {
                throw new NotFoundException($"Categoria '{categoryId}' não encontrada.");
            }

            // evita duplicar ativação em chamadas repetidas
            var existing = await _activeCategoryRepository.GetAsync(teamId, categoryId, cancellationToken);
            if (existing is not null) {
                return;
            }

            await _activeCategoryRepository.AddAsync(new TeamActiveCategory {
                Id          = Guid.NewGuid(),
                TeamId      = teamId,
                CategoryId  = categoryId,
                ActivatedAt = _clock.UtcNow
            }, cancellationToken);
        }

        public async Task DeactivateCategoryAsync(Guid ownerId, Guid teamId, Guid categoryId, CancellationToken cancellationToken = default) {
            await GetOwnedTeamAsync(ownerId, teamId, cancellationToken);

            var existing = await _activeCategoryRepository.GetAsync(teamId, categoryId, cancellationToken);
            if (existing is null) {
                // evita erro ao desativar categoria já inativa
                return;
            }

            await _activeCategoryRepository.RemoveAsync(existing, cancellationToken);
        }

        // desafios sorteados pra hoje (ver DailyChallengeGeneratorService), só os já revelados —
        // não devolve os do dia com RevealAt no futuro, então não existe estado "ainda não abriu"
        // no front, só aparece quando chega a hora
        public async Task<IReadOnlyList<AvailableChallenge>> GetAvailableChallengesAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default) {
            await GetOwnedOrMemberOrAdminTeamAsync(userId, teamId, cancellationToken);

            var now   = _clock.UtcNow;
            var today = DateOnly.FromDateTime(now);

            var todaysEntries = (await _dailyChallengeRepository.GetForTeamAndDateAsync(teamId, today, cancellationToken))
                .Where(e => e.RevealAt <= now)
                .OrderBy(e => e.RevealAt)
                .ToList();

            if (todaysEntries.Count == 0) {
                return Array.Empty<AvailableChallenge>();
            }

            var categoriesById = (await _categoryRepository.GetAllAsync(cancellationToken)).ToDictionary(c => c.Id);

            // usa a submissão mais recente para definir o status exibido
            var latestSubmissionByChallenge = (await _submissionRepository.GetForUserAndTeamAsync(userId, teamId, cancellationToken))
                .GroupBy(s => s.ChallengeId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.CreatedAt).First().Status);

            var result = new List<AvailableChallenge>();
            foreach (var entry in todaysEntries) {
                var challenge = await _challengeRepository.GetByIdAsync(entry.ChallengeId, cancellationToken);
                // ignora entradas cujo desafio foi removido do catálogo depois do sorteio
                if (challenge is null || !categoriesById.TryGetValue(challenge.CategoryId, out var category)) {
                    continue;
                }

                result.Add(new AvailableChallenge(
                    challenge, category, entry.RevealAt,
                    latestSubmissionByChallenge.TryGetValue(challenge.Id, out var status) ? status : null));
            }

            return result;
        }

        public async Task<ChallengeSubmission> SubmitChallengeProofAsync(
            Guid userId, Guid teamId, Guid challengeId, Stream content, string contentType, long contentLength,
            CancellationToken cancellationToken = default) {
            await GetOwnedOrMemberTeamAsync(userId, teamId, cancellationToken);

            var challenge = await _challengeRepository.GetByIdAsync(challengeId, cancellationToken);
            if (challenge is null) {
                throw new NotFoundException($"Desafio '{challengeId}' não encontrado.");
            }

            // só aceita prova de um desafio que é um dos sorteados de hoje pra esse time e já foi
            // revelado — cobre categoria ativa, prazo e rotação diária de uma vez só
            var now   = _clock.UtcNow;
            var today = DateOnly.FromDateTime(now);
            var isTodaysChallenge = (await _dailyChallengeRepository.GetForTeamAndDateAsync(teamId, today, cancellationToken))
                .Any(e => e.ChallengeId == challengeId && e.RevealAt <= now);
            if (!isTodaysChallenge) {
                throw new InvalidChallengeException("Esse desafio não está disponível hoje pra esse time.");
            }

            // apenas pendente ou aprovado bloqueiam novo envio
            var existing = await _submissionRepository.GetActiveForUserChallengeAsync(userId, challengeId, teamId, cancellationToken);
            if (existing is not null) {
                throw new InvalidChallengeException("Você já tem uma submissão pendente ou aprovada para esse desafio nesse time.");
            }

            if (!AllowedSubmissionContentTypes.Contains(contentType)) {
                throw new InvalidChallengeException("Formato de imagem inválido. Use JPG, PNG ou WEBP.");
            }

            if (contentLength <= 0 || contentLength > MaxSubmissionImageBytes) {
                throw new InvalidChallengeException("A imagem deve ter até 3MB.");
            }

            var submissionId = Guid.NewGuid();
            var photoUrl = await _submissionStorage.UploadAsync(submissionId, content, contentType, cancellationToken);

            var submission = new ChallengeSubmission {
                Id          = submissionId,
                ChallengeId = challengeId,
                TeamId      = teamId,
                UserId      = userId,
                PhotoUrl    = photoUrl,
                Status      = ChallengeSubmissionStatus.Pendente,
                CreatedAt   = _clock.UtcNow
            };

            await _submissionRepository.AddAsync(submission, cancellationToken);
            return submission;
        }

        public async Task<IReadOnlyList<PendingSubmission>> GetPendingSubmissionsAsync(Guid callerId, Guid teamId, CancellationToken cancellationToken = default) {
            await GetOwnedTeamAsync(callerId, teamId, cancellationToken);

            var pending = (await _submissionRepository.GetPendingForTeamAsync(teamId, cancellationToken))
                .Where(s => s.Source == ChallengeSource.Time)
                .ToList();
            if (pending.Count == 0) {
                return Array.Empty<PendingSubmission>();
            }

            var challengesById = (await _challengeRepository.GetAllAsync(cancellationToken)).ToDictionary(c => c.Id);
            var submitters     = await LoadUsersAsync(pending.Select(s => s.UserId), cancellationToken);

            var result = new List<PendingSubmission>(pending.Count);
            foreach (var submission in pending) {
                // ignora registros removidos para não quebrar a fila
                if (!challengesById.TryGetValue(submission.ChallengeId, out var challenge) ||
                    !submitters.TryGetValue(submission.UserId, out var submitter)) {
                    continue;
                }
                result.Add(new PendingSubmission(submission, challenge, ToSummary(submitter)));
            }

            return result.OrderBy(p => p.Submission.CreatedAt).ToList();
        }

        public async Task ApproveSubmissionAsync(Guid callerId, Guid teamId, Guid submissionId, CancellationToken cancellationToken = default) {
            var team = await GetOwnedTeamAsync(callerId, teamId, cancellationToken);
            var submission = await GetPendingSubmissionForTeamAsync(teamId, submissionId, cancellationToken);

            // impede auto-aprovação da própria submissão
            if (submission.UserId == callerId) {
                throw new InvalidChallengeException("Você não pode aprovar a própria submissão. Peça para outro membro revisar.");
            }

            var challenge = await _challengeRepository.GetByIdAsync(submission.ChallengeId, cancellationToken);
            if (challenge is null) {
                throw new NotFoundException($"Desafio '{submission.ChallengeId}' não encontrado.");
            }

            submission.Status           = ChallengeSubmissionStatus.Aprovado;
            submission.ReviewedAt       = _clock.UtcNow;
            submission.ReviewedByUserId = callerId;
            await _submissionRepository.UpdateAsync(submission, cancellationToken);

            team.TotalPoints += challenge.Points;
            team.UpdatedAt    = _clock.UtcNow;
            await _teamRepository.UpdateAsync(team, cancellationToken);

            // soma pontos individuais do membro no time ao aprovar
            var memberScore = await _memberScoreRepository.GetAsync(teamId, submission.UserId, cancellationToken);
            if (memberScore is null) {
                await _memberScoreRepository.AddAsync(new TeamMemberScore {
                    Id = Guid.NewGuid(), TeamId = teamId, UserId = submission.UserId,
                    Points = challenge.Points, UpdatedAt = _clock.UtcNow
                }, cancellationToken);
            } else {
                memberScore.Points   += challenge.Points;
                memberScore.UpdatedAt = _clock.UtcNow;
                await _memberScoreRepository.UpdateAsync(memberScore, cancellationToken);
            }

            await _achievementChecker.CheckChallengeCompletedAsync(submission.UserId, cancellationToken);
        }

        public async Task RejectSubmissionAsync(Guid callerId, Guid teamId, Guid submissionId, CancellationToken cancellationToken = default) {
            await GetOwnedTeamAsync(callerId, teamId, cancellationToken);
            var submission = await GetPendingSubmissionForTeamAsync(teamId, submissionId, cancellationToken);

            submission.Status           = ChallengeSubmissionStatus.Recusado;
            submission.ReviewedAt       = _clock.UtcNow;
            submission.ReviewedByUserId = callerId;
            await _submissionRepository.UpdateAsync(submission, cancellationToken);
        }

        public async Task<(Stream Content, string ContentType)> GetSubmissionPhotoAsync(Guid userId, Guid teamId, Guid submissionId, CancellationToken cancellationToken = default) {
            await GetOwnedOrMemberTeamAsync(userId, teamId, cancellationToken);

            var submission = await _submissionRepository.GetByIdAsync(submissionId, cancellationToken);
            if (submission is null || submission.TeamId != teamId) {
                throw new NotFoundException($"Submissão '{submissionId}' não encontrada.");
            }

            return await _submissionStorage.DownloadAsync(submissionId, cancellationToken);
        }

        public async Task<IReadOnlyList<TeamMemberRanking>> GetTeamRankingAsync(Guid callerId, Guid teamId, CancellationToken cancellationToken = default) {
            var team = await GetOwnedOrMemberOrAdminTeamAsync(callerId, teamId, cancellationToken);

            var members        = await _teamMemberRepository.GetByTeamAsync(teamId, cancellationToken);
            var owner          = await _userRepository.GetByIdAsync(team.OwnerId, cancellationToken);
            var memberUsers    = await LoadUsersAsync(members.Select(m => m.UserId), cancellationToken);
            var scoresByUserId = (await _memberScoreRepository.GetByTeamAsync(teamId, cancellationToken))
                .ToDictionary(s => s.UserId, s => s.Points);

            var entries = new List<(UserSummary User, int Points)>();
            if (owner is not null) {
                entries.Add((ToSummary(owner), scoresByUserId.GetValueOrDefault(owner.Id)));
            }
            foreach (var member in members) {
                if (memberUsers.TryGetValue(member.UserId, out var user)) {
                    entries.Add((ToSummary(user), scoresByUserId.GetValueOrDefault(member.UserId)));
                }
            }

            return entries
                .OrderByDescending(e => e.Points)
                .ThenBy(e => e.User.Name)
                .Select((e, index) => new TeamMemberRanking(index + 1, e.User, e.Points))
                .ToList();
        }

        public async Task<IReadOnlyList<AvailableTournamentChallenge>> GetAvailableTournamentChallengesAsync(
            Guid userId, Guid teamId, Guid tournamentId, CancellationToken cancellationToken = default) {
            await GetOwnedOrMemberTeamAsync(userId, teamId, cancellationToken);
            await GetApprovedEntryAsync(teamId, tournamentId, cancellationToken);
            var teamSubmissions = (await _submissionRepository.GetForTeamAsync(teamId, cancellationToken))
                .Where(s => s.TournamentId == tournamentId)
                .ToList();

            var latestMineByChallenge = teamSubmissions
                .Where(s => s.UserId == userId)
                .GroupBy(s => s.ChallengeId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.CreatedAt).First().Status);

            var progressByChallenge = teamSubmissions
                .Where(s => s.Status == ChallengeSubmissionStatus.Aprovado && s.Quantity.HasValue)
                .GroupBy(s => s.ChallengeId)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.Quantity!.Value));

            var now = _clock.UtcNow;
            var result = new List<AvailableTournamentChallenge>();

            var linksByChallenge = (await _tournamentChallengeRepository.GetByTournamentAsync(tournamentId, cancellationToken))
                .ToDictionary(l => l.ChallengeId);
            if (linksByChallenge.Count > 0) {
                var catalogChallenges = await _challengeRepository.GetAllAsync(cancellationToken);
                result.AddRange(catalogChallenges
                    .Where(c => linksByChallenge.ContainsKey(c.Id) && (c.Deadline is null || c.Deadline > now))
                    .Select(c => {
                        var link = linksByChallenge[c.Id];
                        return new AvailableTournamentChallenge(
                            c.Id, c.Title, c.Description, c.Points, ChallengeSource.TorneioCatalogo,
                            link.Goal, link.Unit, link.Goal is not null ? progressByChallenge.GetValueOrDefault(c.Id) : null,
                            latestMineByChallenge.TryGetValue(c.Id, out var catalogStatus) ? catalogStatus : null);
                    }));
            }

            var ownChallenges = await _tournamentOwnChallengeRepository.GetByTournamentAsync(tournamentId, cancellationToken);
            result.AddRange(ownChallenges.Select(c => new AvailableTournamentChallenge(
                c.Id, c.Title, c.Description, c.Points, ChallengeSource.TorneioProprio,
                c.Goal, c.Unit, c.Goal is not null ? progressByChallenge.GetValueOrDefault(c.Id) : null,
                latestMineByChallenge.TryGetValue(c.Id, out var ownStatus) ? ownStatus : null)));

            return result.OrderBy(a => a.Title).ToList();
        }

        public async Task<ChallengeSubmission> SubmitTournamentCatalogChallengeProofAsync(
            Guid userId, Guid teamId, Guid tournamentId, Guid challengeId, decimal? quantity, Stream content, string contentType, long contentLength,
            CancellationToken cancellationToken = default) {
            await GetOwnedOrMemberTeamAsync(userId, teamId, cancellationToken);
            await GetApprovedEntryAsync(teamId, tournamentId, cancellationToken);

            var challenge = await _challengeRepository.GetByIdAsync(challengeId, cancellationToken);
            if (challenge is null) {
                throw new NotFoundException($"Desafio '{challengeId}' não encontrado.");
            }

            var link = await _tournamentChallengeRepository.GetAsync(tournamentId, challengeId, cancellationToken);
            if (link is null) {
                throw new InvalidChallengeException("Esse desafio não está vinculado a esse torneio.");
            }

            if (challenge.Deadline is not null && challenge.Deadline <= _clock.UtcNow) {
                throw new InvalidChallengeException("O prazo desse desafio já passou.");
            }

            var normalizedQuantity = ValidateQuantity(link.Goal, quantity);
            if (link.Goal is null) {
                await EnsureNoActiveTournamentSubmissionAsync(userId, teamId, tournamentId, challengeId, ChallengeSource.TorneioCatalogo, cancellationToken);
            }
            ValidateSubmissionFile(contentType, contentLength);

            return await CreateTournamentSubmissionAsync(
                userId, teamId, tournamentId, challengeId, ChallengeSource.TorneioCatalogo, normalizedQuantity, content, contentType, cancellationToken);
        }

        public async Task<ChallengeSubmission> SubmitTournamentOwnChallengeProofAsync(
            Guid userId, Guid teamId, Guid tournamentId, Guid ownChallengeId, decimal? quantity, Stream content, string contentType, long contentLength,
            CancellationToken cancellationToken = default) {
            await GetOwnedOrMemberTeamAsync(userId, teamId, cancellationToken);
            await GetApprovedEntryAsync(teamId, tournamentId, cancellationToken);

            var challenge = await _tournamentOwnChallengeRepository.GetByIdAsync(ownChallengeId, cancellationToken);
            if (challenge is null || challenge.TournamentId != tournamentId) {
                throw new NotFoundException($"Desafio '{ownChallengeId}' não encontrado.");
            }

            var normalizedQuantity = ValidateQuantity(challenge.Goal, quantity);

            // Mesmo critério do vínculo de catálogo acima: com meta, sem limite de envios.
            if (challenge.Goal is null) {
                await EnsureNoActiveTournamentSubmissionAsync(userId, teamId, tournamentId, ownChallengeId, ChallengeSource.TorneioProprio, cancellationToken);
            }
            ValidateSubmissionFile(contentType, contentLength);

            return await CreateTournamentSubmissionAsync(
                userId, teamId, tournamentId, ownChallengeId, ChallengeSource.TorneioProprio, normalizedQuantity, content, contentType, cancellationToken);
        }

        public async Task<IReadOnlyList<PendingTournamentSubmission>> GetPendingTournamentSubmissionsAsync(
            Guid callerId, Guid teamId, Guid tournamentId, CancellationToken cancellationToken = default) {
            await EnsureTournamentOwnerAsync(callerId, tournamentId, cancellationToken);

            var pending = (await _submissionRepository.GetPendingForTeamAsync(teamId, cancellationToken))
                .Where(s => s.TournamentId == tournamentId)
                .ToList();
            if (pending.Count == 0) {
                return Array.Empty<PendingTournamentSubmission>();
            }

            var catalogById = (await _challengeRepository.GetAllAsync(cancellationToken)).ToDictionary(c => c.Id);
            var ownById     = (await _tournamentOwnChallengeRepository.GetByTournamentAsync(tournamentId, cancellationToken)).ToDictionary(c => c.Id);
            var submitters  = await LoadUsersAsync(pending.Select(s => s.UserId), cancellationToken);

            var result = new List<PendingTournamentSubmission>(pending.Count);
            foreach (var submission in pending) {
                if (!submitters.TryGetValue(submission.UserId, out var submitter)) {
                    continue;
                }

                // ignora registros removidos para não quebrar a fila
                if (submission.Source == ChallengeSource.TorneioCatalogo) {
                    if (!catalogById.TryGetValue(submission.ChallengeId, out var catalogChallenge)) {
                        continue;
                    }
                    result.Add(new PendingTournamentSubmission(submission, catalogChallenge.Title, catalogChallenge.Points, submission.Source, submission.Quantity, ToSummary(submitter)));
                } else if (submission.Source == ChallengeSource.TorneioProprio) {
                    if (!ownById.TryGetValue(submission.ChallengeId, out var ownChallenge)) {
                        continue;
                    }
                    result.Add(new PendingTournamentSubmission(submission, ownChallenge.Title, ownChallenge.Points, submission.Source, submission.Quantity, ToSummary(submitter)));
                }
            }

            return result.OrderBy(p => p.Submission.CreatedAt).ToList();
        }

        public async Task ApproveTournamentSubmissionAsync(Guid callerId, Guid teamId, Guid tournamentId, Guid submissionId, CancellationToken cancellationToken = default) {
            await EnsureTournamentOwnerAsync(callerId, tournamentId, cancellationToken);
            var submission = await GetPendingTournamentSubmissionForTeamAsync(teamId, tournamentId, submissionId, cancellationToken);

            // impede auto-aprovação da própria submissão (ex.: dono do time é também dono do torneio)
            if (submission.UserId == callerId) {
                throw new InvalidChallengeException("Você não pode aprovar a própria submissão. Peça para outro membro revisar.");
            }

            var points = await GetTournamentChallengePointsAsync(submission, cancellationToken);
            if (points is null) {
                throw new NotFoundException($"Desafio '{submission.ChallengeId}' não encontrado.");
            }

            submission.Status           = ChallengeSubmissionStatus.Aprovado;
            submission.ReviewedAt       = _clock.UtcNow;
            submission.ReviewedByUserId = callerId;
            await _submissionRepository.UpdateAsync(submission, cancellationToken);
            var entries = await _tournamentTeamRepository.GetActiveEntriesForTeamAsync(teamId, cancellationToken);
            var entry = entries.FirstOrDefault(e => e.TournamentId == tournamentId && e.Status == TournamentTeamStatus.Aprovado);
            if (entry is not null) {
                entry.Score += points.Value;
                await _tournamentTeamRepository.UpdateAsync(entry, cancellationToken);
            }

            await _achievementChecker.CheckChallengeCompletedAsync(submission.UserId, cancellationToken);
        }

        public async Task RejectTournamentSubmissionAsync(Guid callerId, Guid teamId, Guid tournamentId, Guid submissionId, CancellationToken cancellationToken = default) {
            await EnsureTournamentOwnerAsync(callerId, tournamentId, cancellationToken);
            var submission = await GetPendingTournamentSubmissionForTeamAsync(teamId, tournamentId, submissionId, cancellationToken);

            submission.Status           = ChallengeSubmissionStatus.Recusado;
            submission.ReviewedAt       = _clock.UtcNow;
            submission.ReviewedByUserId = callerId;
            await _submissionRepository.UpdateAsync(submission, cancellationToken);
        }
        private async Task<ChallengeSubmission> GetPendingSubmissionForTeamAsync(Guid teamId, Guid submissionId, CancellationToken cancellationToken) {
            var submission = await _submissionRepository.GetByIdAsync(submissionId, cancellationToken);
            if (submission is null || submission.TeamId != teamId || submission.Source != ChallengeSource.Time) {
                throw new NotFoundException($"Submissão '{submissionId}' não encontrada.");
            }

            if (submission.Status != ChallengeSubmissionStatus.Pendente) {
                throw new InvalidChallengeException("Essa submissão já foi avaliada.");
            }

            return submission;
        }

        private async Task<Dictionary<Guid, User>> LoadUsersAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken) {
            var distinct = ids.Distinct().ToList();
            var users    = await _userRepository.GetByIdsAsync(distinct, cancellationToken);
            return users.ToDictionary(u => u.Id);
        }

        private static UserSummary ToSummary(User user) => new(user.Id, user.Name, user.Username);

        // valida dono do time sem expor gestão de times alheios
        private async Task<Team> GetOwnedTeamAsync(Guid ownerId, Guid teamId, CancellationToken cancellationToken) {
            var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken);
            if (team is null || team.OwnerId != ownerId) {
                throw new NotFoundException("Time não encontrado.");
            }
            return team;
        }

        private async Task<TournamentTeam> GetApprovedEntryAsync(Guid teamId, Guid tournamentId, CancellationToken cancellationToken) {
            var entries = await _tournamentTeamRepository.GetActiveEntriesForTeamAsync(teamId, cancellationToken);
            var entry = entries.FirstOrDefault(e => e.TournamentId == tournamentId && e.Status == TournamentTeamStatus.Aprovado);
            if (entry is null) {
                throw new InvalidChallengeException("Esse time não está Aprovado nesse torneio.");
            }
            return entry;
        }
        private async Task EnsureTournamentOwnerAsync(Guid ownerId, Guid tournamentId, CancellationToken cancellationToken) {
            var tournament = await _tournamentRepository.GetByIdAsync(tournamentId, cancellationToken);
            if (tournament is null || tournament.OwnerId != ownerId) {
                throw new NotFoundException("Torneio não encontrado.");
            }
        }
        private async Task EnsureNoActiveTournamentSubmissionAsync(
            Guid userId, Guid teamId, Guid tournamentId, Guid challengeId, ChallengeSource source, CancellationToken cancellationToken) {
            var hasActive = (await _submissionRepository.GetForUserAndTeamAsync(userId, teamId, cancellationToken))
                .Any(s => s.ChallengeId == challengeId && s.TournamentId == tournamentId && s.Source == source &&
                          s.Status != ChallengeSubmissionStatus.Recusado);
            if (hasActive) {
                throw new InvalidChallengeException("Você já tem uma submissão pendente ou aprovada para esse desafio nesse torneio.");
            }
        }

        private void ValidateSubmissionFile(string contentType, long contentLength) {
            if (!AllowedSubmissionContentTypes.Contains(contentType)) {
                throw new InvalidChallengeException("Formato de imagem inválido. Use JPG, PNG ou WEBP.");
            }

            if (contentLength <= 0 || contentLength > MaxSubmissionImageBytes) {
                throw new InvalidChallengeException("A imagem deve ter até 3MB.");
            }
        }
        private static decimal? ValidateQuantity(decimal? goal, decimal? quantity) {
            if (goal is null) {
                return null;
            }

            if (quantity is null || quantity <= 0) {
                throw new InvalidChallengeException("Esse desafio tem meta configurada — informe a quantidade da sua contribuição.");
            }

            return quantity;
        }

        private async Task<ChallengeSubmission> CreateTournamentSubmissionAsync(
            Guid userId, Guid teamId, Guid tournamentId, Guid challengeId, ChallengeSource source, decimal? quantity,
            Stream content, string contentType, CancellationToken cancellationToken) {
            var submissionId = Guid.NewGuid();
            var photoUrl = await _submissionStorage.UploadAsync(submissionId, content, contentType, cancellationToken);

            var submission = new ChallengeSubmission {
                Id           = submissionId,
                ChallengeId  = challengeId,
                TeamId       = teamId,
                UserId       = userId,
                PhotoUrl     = photoUrl,
                Status       = ChallengeSubmissionStatus.Pendente,
                Source       = source,
                TournamentId = tournamentId,
                Quantity     = quantity,
                CreatedAt    = _clock.UtcNow
            };

            await _submissionRepository.AddAsync(submission, cancellationToken);
            return submission;
        }

        private async Task<ChallengeSubmission> GetPendingTournamentSubmissionForTeamAsync(
            Guid teamId, Guid tournamentId, Guid submissionId, CancellationToken cancellationToken) {
            var submission = await _submissionRepository.GetByIdAsync(submissionId, cancellationToken);
            if (submission is null || submission.TeamId != teamId || submission.TournamentId != tournamentId ||
                submission.Source == ChallengeSource.Time) {
                throw new NotFoundException($"Submissão '{submissionId}' não encontrada.");
            }

            if (submission.Status != ChallengeSubmissionStatus.Pendente) {
                throw new InvalidChallengeException("Essa submissão já foi avaliada.");
            }

            return submission;
        }

        // resolve os pontos do desafio de uma submissão de torneio, seja de catálogo ou próprio
        private async Task<int?> GetTournamentChallengePointsAsync(ChallengeSubmission submission, CancellationToken cancellationToken) {
            if (submission.Source == ChallengeSource.TorneioCatalogo) {
                var challenge = await _challengeRepository.GetByIdAsync(submission.ChallengeId, cancellationToken);
                return challenge?.Points;
            }

            var ownChallenge = await _tournamentOwnChallengeRepository.GetByIdAsync(submission.ChallengeId, cancellationToken);
            return ownChallenge?.Points;
        }

        // valida acesso do usuário ao time — usado nas ações de escrita (ativar/desativar
        // categoria, submeter prova): admin não entra aqui de propósito, só dono ou membro
        // mexem no time de verdade
        private async Task<Team> GetOwnedOrMemberTeamAsync(Guid userId, Guid teamId, CancellationToken cancellationToken) {
            var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken);
            if (team is null) {
                throw new NotFoundException("Time não encontrado.");
            }

            var isMember = team.OwnerId == userId || await _teamMemberRepository.ExistsAsync(teamId, userId, cancellationToken);
            if (!isMember) {
                throw new NotFoundException("Time não encontrado.");
            }

            return team;
        }

        // mesma checagem, mas só pra leitura (desafios disponíveis e ranking): libera também
        // pra admin, que precisa ver essas seções na tela de detalhe de qualquer time pelo
        // painel admin, mesmo não sendo dono nem membro
        private async Task<Team> GetOwnedOrMemberOrAdminTeamAsync(Guid userId, Guid teamId, CancellationToken cancellationToken) {
            var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken);
            if (team is null) {
                throw new NotFoundException("Time não encontrado.");
            }

            var isMember = team.OwnerId == userId || await _teamMemberRepository.ExistsAsync(teamId, userId, cancellationToken);
            if (!isMember) {
                var caller = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (caller is null || !caller.IsAdmin) {
                    throw new NotFoundException("Time não encontrado.");
                }
            }

            return team;
        }
    }
}
