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
        // Mesmos limites do upload de banner de time (TeamService) — nenhuma razão pra prova por
        // foto ter uma regra diferente.
        private static readonly HashSet<string> AllowedSubmissionContentTypes =
            new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

        private const long MaxSubmissionImageBytes = 3 * 1024 * 1024;

        private readonly ITeamRepository                  _teamRepository;
        private readonly ITeamMemberRepository             _teamMemberRepository;
        private readonly IChallengeCategoryRepository      _categoryRepository;
        private readonly IChallengeRepository              _challengeRepository;
        private readonly ITeamActiveCategoryRepository     _activeCategoryRepository;
        private readonly IChallengeSubmissionRepository    _submissionRepository;
        private readonly IChallengeSubmissionStorageService _submissionStorage;
        private readonly IUserRepository                   _userRepository;
        private readonly IClockService                     _clock;

        public TeamChallengeService(
            ITeamRepository                  teamRepository,
            ITeamMemberRepository             teamMemberRepository,
            IChallengeCategoryRepository      categoryRepository,
            IChallengeRepository              challengeRepository,
            ITeamActiveCategoryRepository     activeCategoryRepository,
            IChallengeSubmissionRepository    submissionRepository,
            IChallengeSubmissionStorageService submissionStorage,
            IUserRepository                   userRepository,
            IClockService                     clock) {
            _teamRepository            = teamRepository;
            _teamMemberRepository      = teamMemberRepository;
            _categoryRepository        = categoryRepository;
            _challengeRepository       = challengeRepository;
            _activeCategoryRepository  = activeCategoryRepository;
            _submissionRepository      = submissionRepository;
            _submissionStorage         = submissionStorage;
            _userRepository            = userRepository;
            _clock                     = clock;
        }

        public async Task<IReadOnlyList<TeamCategoryStatus>> GetCategoriesForTeamAsync(Guid ownerId, Guid teamId, CancellationToken cancellationToken = default) {
            await GetOwnedTeamAsync(ownerId, teamId, cancellationToken);

            var categories = await _categoryRepository.GetAllAsync(cancellationToken);
            var activeIds = (await _activeCategoryRepository.GetByTeamAsync(teamId, cancellationToken))
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

            // Idempotente: já ativa, nada a fazer — evita duplicar linha em cliques repetidos.
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
                // Idempotente: já não está ativa.
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

            // Catálogo é curado por admin e pequeno — uma listagem completa filtrada em memória é
            // mais simples que buscar categoria por categoria, mesmo critério de simplicidade do
            // ChallengeCatalogService.
            var categoriesById = (await _categoryRepository.GetAllAsync(cancellationToken))
                .Where(c => activeCategoryIds.Contains(c.Id))
                .ToDictionary(c => c.Id);

            // Uma consulta só, agrupada em memória pelo desafio mais recente — o status mais
            // recente é o que importa pro front decidir "enviar"/"pendente"/"aprovado"/"recusado".
            var latestSubmissionByChallenge = (await _submissionRepository.GetForUserAndTeamAsync(userId, teamId, cancellationToken))
                .GroupBy(s => s.ChallengeId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.CreatedAt).First().Status);

            var now = _clock.UtcNow;
            var result = new List<AvailableChallenge>();

            foreach (var categoryId in activeCategoryIds) {
                // Categoria ativada no passado e removida depois do catálogo: a ativação continua
                // existindo, mas sem categoria pra exibir, não entra na lista.
                if (!categoriesById.TryGetValue(categoryId, out var category)) {
                    continue;
                }

                var challenges = await _challengeRepository.GetByCategoryAsync(categoryId, cancellationToken);

                // Prazo expirado: fora da lista mesmo com a categoria ativa (só desafios SEM prazo
                // ficam sempre disponíveis).
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

            // Ativa = Pendente ou Aprovado. Recusado não bloqueia — é o que permite tentar de novo.
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
                Id        = submissionId,
                ChallengeId = challengeId,
                TeamId    = teamId,
                UserId    = userId,
                PhotoUrl  = photoUrl,
                Status    = ChallengeSubmissionStatus.Pendente,
                CreatedAt = _clock.UtcNow
            };

            await _submissionRepository.AddAsync(submission, cancellationToken);
            return submission;
        }

        public async Task<IReadOnlyList<PendingSubmission>> GetPendingSubmissionsAsync(Guid ownerId, Guid teamId, CancellationToken cancellationToken = default) {
            await GetOwnedTeamAsync(ownerId, teamId, cancellationToken);

            var pending = await _submissionRepository.GetPendingForTeamAsync(teamId, cancellationToken);
            if (pending.Count == 0) {
                return Array.Empty<PendingSubmission>();
            }

            var challengesById = (await _challengeRepository.GetAllAsync(cancellationToken)).ToDictionary(c => c.Id);
            var submitters = await LoadUsersAsync(pending.Select(s => s.UserId), cancellationToken);

            var result = new List<PendingSubmission>(pending.Count);
            foreach (var submission in pending) {
                // Desafio removido do catálogo ou usuário excluído depois do envio: fora da fila
                // em vez de quebrar a listagem inteira.
                if (!challengesById.TryGetValue(submission.ChallengeId, out var challenge) ||
                    !submitters.TryGetValue(submission.UserId, out var submitter)) {
                    continue;
                }
                result.Add(new PendingSubmission(submission, challenge, ToSummary(submitter)));
            }

            return result.OrderBy(p => p.Submission.CreatedAt).ToList();
        }

        public async Task ApproveSubmissionAsync(Guid ownerId, Guid teamId, Guid submissionId, CancellationToken cancellationToken = default) {
            var team = await GetOwnedTeamAsync(ownerId, teamId, cancellationToken);
            var submission = await GetPendingSubmissionForTeamAsync(teamId, submissionId, cancellationToken);

            // Auto-aprovação bloqueada: quem enviou a prova não pode ser quem aprova, mesmo sendo
            // o dono e único aprovador possível hoje. Fica presa em Pendente até outro membro
            // avaliar — sem workaround automático, é a regra pedida.
            if (submission.UserId == ownerId) {
                throw new InvalidChallengeException(
                    "O dono do time não pode aprovar a própria submissão. Peça para outro membro revisar.");
            }

            var challenge = await _challengeRepository.GetByIdAsync(submission.ChallengeId, cancellationToken);
            if (challenge is null) {
                throw new NotFoundException($"Desafio '{submission.ChallengeId}' não encontrado.");
            }

            submission.Status           = ChallengeSubmissionStatus.Aprovado;
            submission.ReviewedAt       = _clock.UtcNow;
            submission.ReviewedByUserId = ownerId;
            await _submissionRepository.UpdateAsync(submission, cancellationToken);

            team.TotalPoints += challenge.Points;
            team.UpdatedAt    = _clock.UtcNow;
            await _teamRepository.UpdateAsync(team, cancellationToken);
        }

        public async Task RejectSubmissionAsync(Guid ownerId, Guid teamId, Guid submissionId, CancellationToken cancellationToken = default) {
            await GetOwnedTeamAsync(ownerId, teamId, cancellationToken);
            var submission = await GetPendingSubmissionForTeamAsync(teamId, submissionId, cancellationToken);

            submission.Status           = ChallengeSubmissionStatus.Recusado;
            submission.ReviewedAt       = _clock.UtcNow;
            submission.ReviewedByUserId = ownerId;
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

        // Carrega a submissão garantindo que ela pertence ao time e ainda está Pendente — usada
        // por aprovar/recusar, que só fazem sentido uma vez por submissão.
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
            var users = await _userRepository.GetByIdsAsync(distinct, cancellationToken);
            return users.ToDictionary(u => u.Id);
        }

        private static UserSummary ToSummary(User user) => new(user.Id, user.Name, user.Username);

        // Mesma guarda de TeamService.GetOwnedTeamAsync — NotFound genérico pra quem não é dono,
        // não revela gestão de time alheio. Duplicada aqui de propósito: é um helper privado do
        // outro serviço, sem ponto de reaproveitamento sem acoplar os dois bounded contexts.
        private async Task<Team> GetOwnedTeamAsync(Guid ownerId, Guid teamId, CancellationToken cancellationToken) {
            var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken);
            if (team is null || team.OwnerId != ownerId) {
                throw new NotFoundException("Time não encontrado.");
            }
            return team;
        }

        // Mesma guarda de TeamService.GetOwnedOrMemberTeamAsync.
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
