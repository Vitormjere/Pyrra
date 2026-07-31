using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly IChallengeSubmissionRepository     _submissionRepository;
        private readonly IChallengeSubmissionStorageService _submissionStorage;
        private readonly ITournamentTeamRepository          _tournamentTeamRepository;
        private readonly ITournamentRepository              _tournamentRepository;
        private readonly ITeamMemberScoreRepository         _memberScoreRepository;
        private readonly IUserRepository                    _userRepository;
        private readonly IClockService                      _clock;

        public TeamChallengeService(
            ITeamRepository                    teamRepository,
            ITeamMemberRepository              teamMemberRepository,
            IChallengeCategoryRepository       categoryRepository,
            IChallengeRepository               challengeRepository,
            ITeamActiveCategoryRepository      activeCategoryRepository,
            IChallengeSubmissionRepository     submissionRepository,
            IChallengeSubmissionStorageService submissionStorage,
            ITournamentTeamRepository          tournamentTeamRepository,
            ITournamentRepository              tournamentRepository,
            ITeamMemberScoreRepository         memberScoreRepository,
            IUserRepository                    userRepository,
            IClockService                      clock) {
            _teamRepository            = teamRepository;
            _teamMemberRepository      = teamMemberRepository;
            _categoryRepository        = categoryRepository;
            _challengeRepository       = challengeRepository;
            _activeCategoryRepository  = activeCategoryRepository;
            _submissionRepository      = submissionRepository;
            _submissionStorage         = submissionStorage;
            _tournamentTeamRepository  = tournamentTeamRepository;
            _tournamentRepository      = tournamentRepository;
            _memberScoreRepository     = memberScoreRepository;
            _userRepository            = userRepository;
            _clock                     = clock;
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

        public async Task<IReadOnlyList<AvailableChallenge>> GetAvailableChallengesAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default) {
            await GetOwnedOrMemberTeamAsync(userId, teamId, cancellationToken);

            var activeCategoryIds = (await _activeCategoryRepository.GetByTeamAsync(teamId, cancellationToken))
                .Select(a => a.CategoryId)
                .ToList();

            if (activeCategoryIds.Count == 0) {
                return Array.Empty<AvailableChallenge>();
            }

            // catálogo pequeno, busca completa em memória simplifica o filtro
            var categoriesById = (await _categoryRepository.GetAllAsync(cancellationToken))
                .Where(c => activeCategoryIds.Contains(c.Id))
                .ToDictionary(c => c.Id);

            // usa a submissão mais recente para definir o status exibido
            var latestSubmissionByChallenge = (await _submissionRepository.GetForUserAndTeamAsync(userId, teamId, cancellationToken))
                .GroupBy(s => s.ChallengeId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.CreatedAt).First().Status);

            var now = _clock.UtcNow;
            var result = new List<AvailableChallenge>();

            foreach (var categoryId in activeCategoryIds) {
                // ignora ativações sem categoria existente no catálogo
                if (!categoriesById.TryGetValue(categoryId, out var category)) {
                    continue;
                }

                var challenges = await _challengeRepository.GetByCategoryAsync(categoryId, cancellationToken);

                // remove desafios expirados e mantém os sem prazo disponíveis
                result.AddRange(challenges
                    .Where(c => c.Deadline is null || c.Deadline > now)
                    .Select(c => new AvailableChallenge(
                        c, category,
                        latestSubmissionByChallenge.TryGetValue(c.Id, out var status) ? status : null)));
            }

            return result
                .OrderBy(a => a.Category.Name)
                .ThenBy(a => a.Challenge.Title)
                .ToList();
        }

        public async Task<ChallengeSubmission> SubmitChallengeProofAsync(
            Guid userId, Guid teamId, Guid challengeId, Stream content, string contentType, long contentLength,
            CancellationToken cancellationToken = default) {
            await GetOwnedOrMemberTeamAsync(userId, teamId, cancellationToken);

            var challenge = await _challengeRepository.GetByIdAsync(challengeId, cancellationToken);
            if (challenge is null) {
                throw new NotFoundException($"Desafio '{challengeId}' não encontrado.");
            }

            var isActive = await _activeCategoryRepository.GetAsync(teamId, challenge.CategoryId, cancellationToken) is not null;
            if (!isActive) {
                throw new InvalidChallengeException("A categoria desse desafio não está ativa nesse time.");
            }

            if (challenge.Deadline is not null && challenge.Deadline <= _clock.UtcNow) {
                throw new InvalidChallengeException("O prazo desse desafio já passou.");
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
            await GetApproverTeamAsync(callerId, teamId, cancellationToken);

            var pending = await _submissionRepository.GetPendingForTeamAsync(teamId, cancellationToken);
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
            var team = await GetApproverTeamAsync(callerId, teamId, cancellationToken);
            var submission = await GetPendingSubmissionForTeamAsync(teamId, submissionId, cancellationToken);

            // impede auto-aprovação da própria submissão
            if (submission.UserId == callerId) {
                throw new InvalidChallengeException(
                    "Você não pode aprovar a própria submissão. Peça para outro membro (ou o dono do torneio, se o time estiver em um) revisar.");
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

            // soma pontos do torneio separadamente quando o time estiver aprovado
            var tournamentEntry = await GetApprovedTournamentEntryAsync(teamId, cancellationToken);
            if (tournamentEntry is not null) {
                tournamentEntry.Score += challenge.Points;
                await _tournamentTeamRepository.UpdateAsync(tournamentEntry, cancellationToken);
            }
        }

        public async Task RejectSubmissionAsync(Guid callerId, Guid teamId, Guid submissionId, CancellationToken cancellationToken = default) {
            await GetApproverTeamAsync(callerId, teamId, cancellationToken);
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
            var team = await GetOwnedOrMemberTeamAsync(callerId, teamId, cancellationToken);

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

        // busca submissão pendente do time para avaliar uma vez
        private async Task<ChallengeSubmission> GetPendingSubmissionForTeamAsync(Guid teamId, Guid submissionId, CancellationToken cancellationToken) {
            var submission = await _submissionRepository.GetByIdAsync(submissionId, cancellationToken);
            if (submission is null || submission.TeamId != teamId) {
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

        // define quem pode aprovar submissões conforme o torneio atual
        private async Task<Team> GetApproverTeamAsync(Guid callerId, Guid teamId, CancellationToken cancellationToken) {
            var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken);
            if (team is null) {
                throw new NotFoundException("Time não encontrado.");
            }

            var approverId = await GetApproverIdAsync(team, cancellationToken);
            if (approverId != callerId) {
                throw new NotFoundException("Time não encontrado.");
            }

            return team;
        }

        private async Task<Guid> GetApproverIdAsync(Team team, CancellationToken cancellationToken) {
            var tournamentEntry = await GetApprovedTournamentEntryAsync(team.Id, cancellationToken);
            if (tournamentEntry is null) {
                return team.OwnerId;
            }

            var tournament = await _tournamentRepository.GetByIdAsync(tournamentEntry.TournamentId, cancellationToken);
            // volta ao dono do time se o torneio não existir mais
            return tournament?.OwnerId ?? team.OwnerId;
        }

        // busca torneio aprovado em que o time participa
        private async Task<TournamentTeam?> GetApprovedTournamentEntryAsync(Guid teamId, CancellationToken cancellationToken) {
            var entry = await _tournamentTeamRepository.GetActiveForTeamAsync(teamId, cancellationToken);
            return entry?.Status == TournamentTeamStatus.Aprovado ? entry : null;
        }

        // valida acesso do usuário ao time
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
    }
}
