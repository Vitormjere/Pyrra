using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Comunidade;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Comunidade {
    public class TournamentService : ITournamentService {
        // Mesmas regras do banner de time (mantém o padrão de validação entre os dois recursos)
        private static readonly HashSet<string> AllowedBannerContentTypes =
            new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

        private const long MaxBannerImageBytes = 3 * 1024 * 1024;

        private readonly ITournamentRepository           _tournamentRepository;
        private readonly ITournamentRequestRepository    _requestRepository;
        private readonly ITournamentTeamRepository       _tournamentTeamRepository;
        private readonly ITeamRepository                 _teamRepository;
        private readonly IUserRepository                 _userRepository;
        private readonly ITournamentBannerStorageService _bannerStorage;
        private readonly IAdminAuthorizationService      _adminAuth;
        private readonly IClockService                   _clock;

        public TournamentService(
            ITournamentRepository           tournamentRepository,
            ITournamentRequestRepository    requestRepository,
            ITournamentTeamRepository       tournamentTeamRepository,
            ITeamRepository                 teamRepository,
            IUserRepository                 userRepository,
            ITournamentBannerStorageService bannerStorage,
            IAdminAuthorizationService      adminAuth,
            IClockService clock) {
            _tournamentRepository     = tournamentRepository;
            _requestRepository        = requestRepository;
            _tournamentTeamRepository = tournamentTeamRepository;
            _teamRepository           = teamRepository;
            _userRepository           = userRepository;
            _bannerStorage            = bannerStorage;
            _adminAuth                = adminAuth;
            _clock                    = clock;
        }

        public async Task<TournamentSummary> CreateOfficialAsync(
            Guid adminUserId, string name, string? description, TeamBannerTheme bannerTheme,
            CancellationToken cancellationToken = default) {
            await _adminAuth.EnsureAdminAsync(adminUserId, cancellationToken);

            var tournament = await CreateTournamentAsync(adminUserId, name, description, bannerTheme, cancellationToken);
            return await ToSummaryAsync(tournament, adminUserId, cancellationToken);
        }

        public async Task<TournamentRequestSummary> RequestTournamentAsync(
            Guid requesterId, string proposedName, string? proposedDescription,
            CancellationToken cancellationToken = default) {
            var normalizedName = proposedName?.Trim();
            if (string.IsNullOrEmpty(normalizedName)) {
                throw new InvalidTournamentException("O nome do torneio é obrigatório.");
            }

            var request = new TournamentRequest {
                Id                  = Guid.NewGuid(),
                RequesterId         = requesterId,
                ProposedName        = normalizedName,
                ProposedDescription = string.IsNullOrWhiteSpace(proposedDescription) ? null : proposedDescription.Trim(),
                Status              = TournamentRequestStatus.Pendente,
                CreatedAt           = _clock.UtcNow
            };

            await _requestRepository.AddAsync(request, cancellationToken);

            var requester = await _userRepository.GetByIdAsync(requesterId, cancellationToken);
            if (requester is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }

            return new TournamentRequestSummary(request.Id, request.ProposedName, request.ProposedDescription, ToSummary(requester), request.CreatedAt);
        }

        public async Task<IReadOnlyList<TournamentRequestSummary>> GetPendingRequestsAsync(Guid adminUserId, CancellationToken cancellationToken = default) {
            await _adminAuth.EnsureAdminAsync(adminUserId, cancellationToken);

            var pending = await _requestRepository.GetPendingAsync(cancellationToken);
            if (pending.Count == 0) {
                return Array.Empty<TournamentRequestSummary>();
            }

            var requesters = await LoadUsersAsync(pending.Select(r => r.RequesterId), cancellationToken);

            var result = new List<TournamentRequestSummary>(pending.Count);
            foreach (var request in pending) {
                // Solicitante removido: ignora para não quebrar a listagem
                if (!requesters.TryGetValue(request.RequesterId, out var requester)) {
                    continue;
                }
                result.Add(new TournamentRequestSummary(request.Id, request.ProposedName, request.ProposedDescription, ToSummary(requester), request.CreatedAt));
            }

            return result.OrderBy(r => r.CreatedAt).ToList();
        }

        public async Task<TournamentSummary> ApproveRequestAsync(Guid adminUserId, Guid requestId, CancellationToken cancellationToken = default) {
            await _adminAuth.EnsureAdminAsync(adminUserId, cancellationToken);

            var request = await GetPendingRequestAsync(requestId, cancellationToken);

            var tournament = await CreateTournamentAsync(
                request.RequesterId, request.ProposedName, request.ProposedDescription, TeamBannerTheme.Verde, cancellationToken);

            request.Status              = TournamentRequestStatus.Aprovado;
            request.ReviewedAt          = _clock.UtcNow;
            request.ReviewedByUserId    = adminUserId;
            request.CreatedTournamentId = tournament.Id;
            await _requestRepository.UpdateAsync(request, cancellationToken);

            return await ToSummaryAsync(tournament, adminUserId, cancellationToken);
        }

        public async Task RejectRequestAsync(Guid adminUserId, Guid requestId, CancellationToken cancellationToken = default) {
            await _adminAuth.EnsureAdminAsync(adminUserId, cancellationToken);

            var request = await GetPendingRequestAsync(requestId, cancellationToken);

            request.Status           = TournamentRequestStatus.Recusado;
            request.ReviewedAt       = _clock.UtcNow;
            request.ReviewedByUserId = adminUserId;
            await _requestRepository.UpdateAsync(request, cancellationToken);
        }

        public async Task<IReadOnlyList<TournamentSummary>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default) {
            var tournaments = await _tournamentRepository.GetAllAsync(cancellationToken);
            return await ToSummariesAsync(tournaments, userId, cancellationToken);
        }

        public async Task<IReadOnlyList<TournamentSummary>> GetMyTournamentsAsync(Guid userId, CancellationToken cancellationToken = default) {
            var tournaments = await _tournamentRepository.GetOwnedByUserAsync(userId, cancellationToken);
            return await ToSummariesAsync(tournaments, userId, cancellationToken);
        }

        public async Task<TournamentDetails> GetDetailsAsync(Guid userId, Guid tournamentId, CancellationToken cancellationToken = default) {
            var tournament = await GetTournamentAsync(tournamentId, cancellationToken);
            var summary   = await ToSummaryAsync(tournament, userId, cancellationToken);
            return new TournamentDetails(summary, tournament.InviteToken);
        }

        public async Task<TournamentSummary> SetBannerThemeAsync(Guid ownerId, Guid tournamentId, TeamBannerTheme bannerTheme, CancellationToken cancellationToken = default) {
            var tournament = await GetOwnedTournamentAsync(ownerId, tournamentId, cancellationToken);

            tournament.BannerTheme = bannerTheme;
            tournament.UpdatedAt   = _clock.UtcNow;
            await _tournamentRepository.UpdateAsync(tournament, cancellationToken);

            return await ToSummaryAsync(tournament, ownerId, cancellationToken);
        }

        public async Task<TournamentSummary> SetBannerImageAsync(
            Guid ownerId, Guid tournamentId, Stream content, string contentType, long contentLength,
            CancellationToken cancellationToken = default) {
            var tournament = await GetOwnedTournamentAsync(ownerId, tournamentId, cancellationToken);

            if (!AllowedBannerContentTypes.Contains(contentType)) {
                throw new InvalidTournamentException("Formato de imagem inválido. Use JPG, PNG ou WEBP.");
            }

            if (contentLength <= 0 || contentLength > MaxBannerImageBytes) {
                throw new InvalidTournamentException("A imagem deve ter até 3MB.");
            }

            var url = await _bannerStorage.UploadAsync(tournamentId, content, contentType, cancellationToken);

            tournament.BannerImageUrl = url;
            tournament.UpdatedAt      = _clock.UtcNow;
            await _tournamentRepository.UpdateAsync(tournament, cancellationToken);

            return await ToSummaryAsync(tournament, ownerId, cancellationToken);
        }

        public async Task<TournamentSummary> RemoveBannerImageAsync(Guid ownerId, Guid tournamentId, CancellationToken cancellationToken = default) {
            var tournament = await GetOwnedTournamentAsync(ownerId, tournamentId, cancellationToken);

            if (tournament.BannerImageUrl is not null) {
                await _bannerStorage.DeleteAsync(tournamentId, cancellationToken);
                tournament.BannerImageUrl = null;
                tournament.UpdatedAt      = _clock.UtcNow;
                await _tournamentRepository.UpdateAsync(tournament, cancellationToken);
            }

            return await ToSummaryAsync(tournament, ownerId, cancellationToken);
        }

        public async Task<TournamentTeamSummary> RequestTeamEntryAsync(Guid teamOwnerId, Guid teamId, Guid tournamentId, CancellationToken cancellationToken = default) {
            var tournament = await GetTournamentAsync(tournamentId, cancellationToken);
            return await RequestEntryCoreAsync(teamOwnerId, teamId, tournament, cancellationToken);
        }

        public async Task<TournamentTeamSummary> RequestTeamEntryViaInviteAsync(Guid teamOwnerId, Guid teamId, string inviteToken, CancellationToken cancellationToken = default) {
            var tournament = await _tournamentRepository.GetByInviteTokenAsync(inviteToken, cancellationToken);
            if (tournament is null) {
                throw new NotFoundException("Convite inválido ou expirado.");
            }
            return await RequestEntryCoreAsync(teamOwnerId, teamId, tournament, cancellationToken);
        }

        public async Task<IReadOnlyList<TournamentTeamSummary>> GetPendingEntriesAsync(Guid tournamentOwnerId, Guid tournamentId, CancellationToken cancellationToken = default) {
            await GetOwnedTournamentAsync(tournamentOwnerId, tournamentId, cancellationToken);

            var pending = await _tournamentTeamRepository.GetPendingForTournamentAsync(tournamentId, cancellationToken);
            return await ToTournamentTeamSummariesAsync(pending, cancellationToken);
        }

        public async Task ApproveEntryAsync(Guid tournamentOwnerId, Guid tournamentId, Guid tournamentTeamId, CancellationToken cancellationToken = default) {
            var entry = await GetPendingEntryForOwnerAsync(tournamentOwnerId, tournamentId, tournamentTeamId, cancellationToken);

            entry.Status           = TournamentTeamStatus.Aprovado;
            entry.ReviewedAt       = _clock.UtcNow;
            entry.ReviewedByUserId = tournamentOwnerId;
            await _tournamentTeamRepository.UpdateAsync(entry, cancellationToken);
        }

        public async Task RejectEntryAsync(Guid tournamentOwnerId, Guid tournamentId, Guid tournamentTeamId, CancellationToken cancellationToken = default) {
            var entry = await GetPendingEntryForOwnerAsync(tournamentOwnerId, tournamentId, tournamentTeamId, cancellationToken);

            entry.Status           = TournamentTeamStatus.Recusado;
            entry.ReviewedAt       = _clock.UtcNow;
            entry.ReviewedByUserId = tournamentOwnerId;
            await _tournamentTeamRepository.UpdateAsync(entry, cancellationToken);
        }

        public async Task<IReadOnlyList<TournamentTeamSummary>> GetRankingAsync(Guid tournamentId, CancellationToken cancellationToken = default) {
            await GetTournamentAsync(tournamentId, cancellationToken);

            var approved  = await _tournamentTeamRepository.GetApprovedForTournamentAsync(tournamentId, cancellationToken);
            var summaries = await ToTournamentTeamSummariesAsync(approved, cancellationToken);

            return summaries
                .OrderByDescending(t => t.Score)
                .ThenBy(t => t.TeamName)
                .ToList();
        }

        // ---- internos ----

        private async Task<Tournament> CreateTournamentAsync(
            Guid ownerId, string name, string? description, TeamBannerTheme bannerTheme, CancellationToken cancellationToken) {
            var normalizedName = name?.Trim();
            if (string.IsNullOrEmpty(normalizedName)) {
                throw new InvalidTournamentException("O nome do torneio é obrigatório.");
            }

            var tournament = new Tournament {
                Id          = Guid.NewGuid(),
                Name        = normalizedName,
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                OwnerId     = ownerId,
                InviteToken = Guid.NewGuid().ToString("N"),
                BannerTheme = bannerTheme,
                CreatedAt   = _clock.UtcNow,
                UpdatedAt   = _clock.UtcNow
            };

            await _tournamentRepository.AddAsync(tournament, cancellationToken);
            return tournament;
        }

        private async Task<IReadOnlyList<TournamentSummary>> ToSummariesAsync(IReadOnlyList<Tournament> tournaments, Guid userId, CancellationToken cancellationToken) {
            if (tournaments.Count == 0) {
                return Array.Empty<TournamentSummary>();
            }

            var results = new List<TournamentSummary>(tournaments.Count);
            foreach (var tournament in tournaments) {
                results.Add(await ToSummaryAsync(tournament, userId, cancellationToken));
            }

            return results.OrderBy(t => t.Name).ToList();
        }

        private async Task<TournamentSummary> ToSummaryAsync(Tournament tournament, Guid userId, CancellationToken cancellationToken) {
            var owner = await _userRepository.GetByIdAsync(tournament.OwnerId, cancellationToken);
            if (owner is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }

            return new TournamentSummary(
                tournament.Id,
                tournament.Name,
                tournament.Description,
                ToSummary(owner),
                tournament.OwnerId == userId,
                tournament.BannerTheme,
                tournament.BannerImageUrl);
        }

        private async Task<Tournament> GetTournamentAsync(Guid tournamentId, CancellationToken cancellationToken) {
            var tournament = await _tournamentRepository.GetByIdAsync(tournamentId, cancellationToken);
            if (tournament is null) {
                throw new NotFoundException("Torneio não encontrado.");
            }
            return tournament;
        }

        // Garante que o usuário é dono. Quem não for recebe NotFound genérico
        private async Task<Tournament> GetOwnedTournamentAsync(Guid ownerId, Guid tournamentId, CancellationToken cancellationToken) {
            var tournament = await _tournamentRepository.GetByIdAsync(tournamentId, cancellationToken);
            if (tournament is null || tournament.OwnerId != ownerId) {
                throw new NotFoundException("Torneio não encontrado.");
            }
            return tournament;
        }

        private async Task<TournamentRequest> GetPendingRequestAsync(Guid requestId, CancellationToken cancellationToken) {
            var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken);
            if (request is null) {
                throw new NotFoundException($"Solicitação '{requestId}' não encontrada.");
            }

            if (request.Status != TournamentRequestStatus.Pendente) {
                throw new InvalidTournamentException("Essa solicitação já foi avaliada.");
            }

            return request;
        }

        // Lógica compartilhada: só muda como o torneio é encontrado antes de chegar aqui
        private async Task<TournamentTeamSummary> RequestEntryCoreAsync(Guid teamOwnerId, Guid teamId, Tournament tournament, CancellationToken cancellationToken) {
            var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken);
            if (team is null || team.OwnerId != teamOwnerId) {
                throw new NotFoundException("Time não encontrado.");
            }

            // Recusados não contam nem aqui nem no limite abaixo — podem tentar de novo livremente
            var activeEntries = await _tournamentTeamRepository.GetActiveEntriesForTeamAsync(teamId, cancellationToken);

            if (activeEntries.Any(e => e.TournamentId == tournament.Id)) {
                throw new InvalidTournamentException("Esse time já está nesse torneio ou tem uma solicitação pendente nele.");
            }

            if (activeEntries.Count >= TournamentTeam.MaxTournamentsPerTeam) {
                throw new InvalidTournamentException($"Esse time já atingiu o limite de {TournamentTeam.MaxTournamentsPerTeam} torneios simultâneos.");
            }

            var entry = new TournamentTeam {
                Id           = Guid.NewGuid(),
                TournamentId = tournament.Id,
                TeamId       = teamId,
                Status       = TournamentTeamStatus.Pendente,
                Score        = 0,
                RequestedAt  = _clock.UtcNow
            };

            await _tournamentTeamRepository.AddAsync(entry, cancellationToken);
            return ToTournamentTeamSummary(entry, team);
        }

        // Garante que a entrada é pendente e pertence ao torneio antes de aprovar ou recusar
        private async Task<TournamentTeam> GetPendingEntryForOwnerAsync(Guid tournamentOwnerId, Guid tournamentId, Guid tournamentTeamId, CancellationToken cancellationToken) {
            var entry = await _tournamentTeamRepository.GetByIdAsync(tournamentTeamId, cancellationToken);
            if (entry is null || entry.TournamentId != tournamentId) {
                throw new NotFoundException($"Solicitação de entrada '{tournamentTeamId}' não encontrada.");
            }

            // Torneio inexistente ou de outro dono retorna NotFound genérico
            await GetOwnedTournamentAsync(tournamentOwnerId, tournamentId, cancellationToken);

            if (entry.Status != TournamentTeamStatus.Pendente) {
                throw new InvalidTournamentException("Essa solicitação já foi avaliada.");
            }

            return entry;
        }

        private async Task<IReadOnlyList<TournamentTeamSummary>> ToTournamentTeamSummariesAsync(IReadOnlyList<TournamentTeam> entries, CancellationToken cancellationToken) {
            if (entries.Count == 0) {
                return Array.Empty<TournamentTeamSummary>();
            }

            var result = new List<TournamentTeamSummary>(entries.Count);
            foreach (var entry in entries) {
                // Time removido: ignora para manter a listagem
                var team = await _teamRepository.GetByIdAsync(entry.TeamId, cancellationToken);
                if (team is null) {
                    continue;
                }
                result.Add(ToTournamentTeamSummary(entry, team));
            }

            return result;
        }

        private static TournamentTeamSummary ToTournamentTeamSummary(TournamentTeam entry, Team team) => new(
            entry.Id, entry.TournamentId, team.Id, team.Name, team.BannerTheme, team.BannerImageUrl,
            entry.Status, entry.Score, entry.RequestedAt);

        private async Task<Dictionary<Guid, User>> LoadUsersAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken) {
            var distinct = ids.Distinct().ToList();
            var users = await _userRepository.GetByIdsAsync(distinct, cancellationToken);
            return users.ToDictionary(u => u.Id);
        }

        private static UserSummary ToSummary(User user) => new(user.Id, user.Name, user.Username);
    }
}