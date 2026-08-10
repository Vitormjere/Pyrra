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
    public class TeamService : ITeamService {
        // formatos e tamanho máximo permitidos pro upload do banner
        private static readonly HashSet<string> AllowedBannerContentTypes =
            new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

        private const long MaxBannerImageBytes = 3 * 1024 * 1024;

        private readonly ITeamRepository            _teamRepository;
        private readonly ITeamMemberRepository      _teamMemberRepository;
        private readonly ITeamInviteRepository      _teamInviteRepository;
        private readonly IFriendshipRepository      _friendshipRepository;
        private readonly IUserRepository            _userRepository;
        private readonly ITeamBannerStorageService  _bannerStorage;
        private readonly IClockService              _clock;
        private readonly ITournamentTeamRepository  _tournamentTeamRepository;
        private readonly ITournamentRepository      _tournamentRepository;
        private readonly IAdminAuthorizationService _adminAuth;

        public TeamService(
            ITeamRepository            teamRepository,
            ITeamMemberRepository      teamMemberRepository,
            ITeamInviteRepository      teamInviteRepository,
            IFriendshipRepository      friendshipRepository,
            IUserRepository            userRepository,
            ITeamBannerStorageService  bannerStorage,
            IClockService              clock,
            ITournamentTeamRepository  tournamentTeamRepository,
            ITournamentRepository      tournamentRepository,
            IAdminAuthorizationService adminAuth) {
            _teamRepository           = teamRepository;
            _teamMemberRepository     = teamMemberRepository;
            _teamInviteRepository     = teamInviteRepository;
            _friendshipRepository     = friendshipRepository;
            _userRepository           = userRepository;
            _bannerStorage            = bannerStorage;
            _clock                    = clock;
            _tournamentTeamRepository = tournamentTeamRepository;
            _tournamentRepository     = tournamentRepository;
            _adminAuth                = adminAuth;
        }

        public async Task<TeamSummary> CreateAsync(
            Guid ownerId,
            string name,
            string? description,
            int memberLimit,
            TeamVisibility visibility = TeamVisibility.Privado,
            TeamBannerTheme bannerTheme = TeamBannerTheme.Verde,
            CancellationToken cancellationToken = default) {
            if (memberLimit <= 0) {
                throw new InvalidTeamException("O limite de membros precisa ser maior que zero.");
            }

            var team = new Team {
                Id          = Guid.NewGuid(),
                Name        = name.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                OwnerId     = ownerId,
                MemberLimit = memberLimit,
                InviteToken = Guid.NewGuid().ToString("N"),
                TotalPoints = 0,
                Visibility  = visibility,
                BannerTheme = bannerTheme,
                CreatedAt   = _clock.UtcNow,
                UpdatedAt   = _clock.UtcNow
            };

            await _teamRepository.AddAsync(team, cancellationToken);
            return await ToSummaryAsync(team, ownerId, cancellationToken);
        }

        public async Task<IReadOnlyList<TeamSummary>> GetMyTeamsAsync(Guid userId, CancellationToken cancellationToken = default) {
            var teams = await _teamRepository.GetForUserAsync(userId, cancellationToken);
            return await ToSummariesAsync(teams, userId, cancellationToken);
        }

        public async Task<IReadOnlyList<TeamSummary>> GetMyEligibleForTournamentAsync(Guid userId, CancellationToken cancellationToken = default) {
            var teams = await _teamRepository.GetForUserAsync(userId, cancellationToken);
            var owned = teams.Where(t => t.OwnerId == userId).ToList();
            if (owned.Count == 0) {
                return Array.Empty<TeamSummary>();
            }

            var results = new List<TeamSummary>();
            foreach (var team in owned) {
                var activeEntries = await _tournamentTeamRepository.GetActiveEntriesForTeamAsync(team.Id, cancellationToken);
                if (activeEntries.Count < TournamentTeam.MaxTournamentsPerTeam) {
                    var summary = await TryToSummaryAsync(team, userId, cancellationToken);
                    if (summary is not null) {
                        results.Add(summary);
                    }
                }
            }

            return results.OrderBy(t => t.Name).ToList();
        }

        public async Task<IReadOnlyList<PublicTeamSummary>> GetPublicTeamsAsync(Guid userId, CancellationToken cancellationToken = default) {
            var teams = await _teamRepository.GetPublicAsync(cancellationToken);
            if (teams.Count == 0) {
                return Array.Empty<PublicTeamSummary>();
            }

            var results = new List<PublicTeamSummary>(teams.Count);
            foreach (var team in teams) {
                // dono removido (soft delete): ignora pra não quebrar a listagem pública por causa de um time órfão — mesmo critério de TournamentService
                var summary = await TryToSummaryAsync(team, userId, cancellationToken);
                if (summary is not null) {
                    results.Add(new PublicTeamSummary(summary, team.InviteToken));
                }
            }

            return results;
        }

        public async Task<IReadOnlyList<TeamSummary>> GetAllTeamsAsync(Guid callerId, CancellationToken cancellationToken = default) {
            await _adminAuth.EnsureAdminAsync(callerId, cancellationToken);

            var teams = await _teamRepository.GetAllAsync(cancellationToken);
            return await ToSummariesAsync(teams, callerId, cancellationToken);
        }

        public async Task SetVisibilityAsync(Guid ownerId, Guid teamId, TeamVisibility visibility, CancellationToken cancellationToken = default) {
            var team = await GetOwnedTeamAsync(ownerId, teamId, cancellationToken);

            team.Visibility = visibility;
            team.UpdatedAt  = _clock.UtcNow;
            await _teamRepository.UpdateAsync(team, cancellationToken);
        }

        public async Task<TeamSummary> SetBannerThemeAsync(Guid ownerId, Guid teamId, TeamBannerTheme bannerTheme, CancellationToken cancellationToken = default) {
            var team = await GetOwnedTeamAsync(ownerId, teamId, cancellationToken);

            team.BannerTheme = bannerTheme;
            team.UpdatedAt   = _clock.UtcNow;
            await _teamRepository.UpdateAsync(team, cancellationToken);

            return await ToSummaryAsync(team, ownerId, cancellationToken);
        }

        public async Task<TeamSummary> SetBannerImageAsync(
            Guid ownerId,
            Guid teamId,
            Stream content,
            string contentType,
            long contentLength,
            CancellationToken cancellationToken = default) {
            var team = await GetOwnedTeamAsync(ownerId, teamId, cancellationToken);

            if (!AllowedBannerContentTypes.Contains(contentType)) {
                throw new InvalidTeamException("Formato de imagem inválido. Use JPG, PNG ou WEBP.");
            }

            if (contentLength <= 0 || contentLength > MaxBannerImageBytes) {
                throw new InvalidTeamException("A imagem deve ter até 3MB.");
            }

            var url = await _bannerStorage.UploadAsync(teamId, content, contentType, cancellationToken);

            team.BannerImageUrl = url;
            team.UpdatedAt      = _clock.UtcNow;
            await _teamRepository.UpdateAsync(team, cancellationToken);

            return await ToSummaryAsync(team, ownerId, cancellationToken);
        }

        public async Task<TeamSummary> RemoveBannerImageAsync(Guid ownerId, Guid teamId, CancellationToken cancellationToken = default) {
            var team = await GetOwnedTeamAsync(ownerId, teamId, cancellationToken);

            if (team.BannerImageUrl is not null) {
                await _bannerStorage.DeleteAsync(teamId, cancellationToken);
                team.BannerImageUrl = null;
                team.UpdatedAt      = _clock.UtcNow;
                await _teamRepository.UpdateAsync(team, cancellationToken);
            }

            return await ToSummaryAsync(team, ownerId, cancellationToken);
        }

        public async Task<TeamDetails> GetDetailsAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default) {
            var team = await GetOwnedOrMemberTeamAsync(userId, teamId, cancellationToken);

            var summary = await ToSummaryAsync(team, userId, cancellationToken);
            var members = await _teamMemberRepository.GetByTeamAsync(teamId, cancellationToken);

            var owner = await _userRepository.GetByIdAsync(team.OwnerId, cancellationToken);
            var memberUsers = await LoadUsersAsync(members.Select(m => m.UserId), cancellationToken);

            var list = new List<TeamMemberSummary>(members.Count + 1);
            if (owner is not null) {
                list.Add(new TeamMemberSummary(owner.Id, ToSummary(owner), true, null));
            }

            foreach (var member in members) {
                if (memberUsers.TryGetValue(member.UserId, out var user)) {
                    list.Add(new TeamMemberSummary(user.Id, ToSummary(user), false, member.JoinedAt));
                }
            }

            var activeTournaments = await GetActiveTournamentsAsync(teamId, cancellationToken);

            return new TeamDetails(summary, list, team.InviteToken, activeTournaments);
        }

        public async Task InviteFriendAsync(Guid ownerId, Guid teamId, Guid inviteeId, CancellationToken cancellationToken = default) {
            var team = await GetOwnedTeamAsync(ownerId, teamId, cancellationToken);

            if (inviteeId == ownerId) {
                throw new InvalidTeamException("Você não pode convidar a si mesmo.");
            }

            // só amigo confirmado pode ser convidado
            var friendship = await _friendshipRepository.GetBetweenAsync(ownerId, inviteeId, cancellationToken);
            if (friendship is null || friendship.Status != FriendshipStatus.Aceito) {
                throw new InvalidTeamException("Só é possível convidar amigos confirmados.");
            }

            if (await _teamMemberRepository.ExistsAsync(teamId, inviteeId, cancellationToken)) {
                throw new InvalidTeamException("Esse usuário já é membro do time.");
            }

            var existing = await _teamInviteRepository.GetByTeamAndInviteeAsync(teamId, inviteeId, cancellationToken);
            if (existing is not null) {
                switch (existing.Status) {
                    case TeamInviteStatus.Pendente:
                        throw new InvalidTeamException("Esse usuário já tem um convite pendente para esse time.");
                    case TeamInviteStatus.Aceito:
                        throw new InvalidTeamException("Esse usuário já é membro do time.");
                    case TeamInviteStatus.Recusado:
                        // convite recusado pode ser reenviado
                        existing.InviterId  = ownerId;
                        existing.Status     = TeamInviteStatus.Pendente;
                        existing.CreatedAt  = _clock.UtcNow;
                        existing.RespondedAt = null;
                        await _teamInviteRepository.UpdateAsync(existing, cancellationToken);
                        return;
                }
            }

            await _teamInviteRepository.AddAsync(new TeamInvite {
                Id         = Guid.NewGuid(),
                TeamId     = teamId,
                InviterId  = ownerId,
                InviteeId  = inviteeId,
                Status     = TeamInviteStatus.Pendente,
                CreatedAt  = _clock.UtcNow
            }, cancellationToken);
        }

        public async Task<IReadOnlyList<TeamInviteSummary>> GetPendingReceivedInvitesAsync(Guid userId, CancellationToken cancellationToken = default) {
            var pending = await _teamInviteRepository.GetPendingReceivedAsync(userId, cancellationToken);
            if (pending.Count == 0) {
                return Array.Empty<TeamInviteSummary>();
            }

            var inviters = await LoadUsersAsync(pending.Select(i => i.InviterId), cancellationToken);

            var results = new List<TeamInviteSummary>(pending.Count);
            foreach (var invite in pending) {
                var team = await _teamRepository.GetByIdAsync(invite.TeamId, cancellationToken);
                if (team is null || !inviters.TryGetValue(invite.InviterId, out var inviter)) {
                    continue;
                }

                // dono do time removido (soft delete): ignora igual aos demais casos, em vez de derrubar a lista inteira de convites por causa de um time órfão
                var summary = await TryToSummaryAsync(team, userId, cancellationToken);
                if (summary is null) {
                    continue;
                }
                results.Add(new TeamInviteSummary(invite.Id, summary, ToSummary(inviter), invite.CreatedAt));
            }

            return results;
        }

        public Task<int> GetPendingReceivedInvitesCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
            _teamInviteRepository.CountPendingReceivedAsync(userId, cancellationToken);

        public async Task AcceptInviteAsync(Guid userId, Guid inviteId, CancellationToken cancellationToken = default) {
            var invite = await GetPendingAddressedToAsync(userId, inviteId, cancellationToken);

            var team = await _teamRepository.GetByIdAsync(invite.TeamId, cancellationToken);
            if (team is null) {
                throw new NotFoundException("Time não encontrado.");
            }

            var memberCount = await _teamMemberRepository.CountByTeamAsync(team.Id, cancellationToken);
            if (1 + memberCount >= team.MemberLimit) {
                throw new InvalidTeamException("Esse time já está cheio.");
            }

            await _teamMemberRepository.AddAsync(new TeamMember {
                Id       = Guid.NewGuid(),
                TeamId   = team.Id,
                UserId   = userId,
                JoinedAt = _clock.UtcNow
            }, cancellationToken);

            invite.Status      = TeamInviteStatus.Aceito;
            invite.RespondedAt = _clock.UtcNow;
            await _teamInviteRepository.UpdateAsync(invite, cancellationToken);
        }

        public async Task DeclineInviteAsync(Guid userId, Guid inviteId, CancellationToken cancellationToken = default) {
            var invite = await GetPendingAddressedToAsync(userId, inviteId, cancellationToken);
            invite.Status      = TeamInviteStatus.Recusado;
            invite.RespondedAt = _clock.UtcNow;
            await _teamInviteRepository.UpdateAsync(invite, cancellationToken);
        }

        public async Task<JoinResult> JoinViaLinkAsync(Guid userId, string inviteToken, CancellationToken cancellationToken = default) {
            var team = await _teamRepository.GetByInviteTokenAsync(inviteToken, cancellationToken);
            if (team is null) {
                throw new NotFoundException("Convite inválido ou expirado.");
            }

            if (team.OwnerId == userId) {
                return new JoinResult(await ToSummaryAsync(team, userId, cancellationToken), JoinOutcome.OwnLink);
            }

            if (await _teamMemberRepository.ExistsAsync(team.Id, userId, cancellationToken)) {
                return new JoinResult(await ToSummaryAsync(team, userId, cancellationToken), JoinOutcome.AlreadyMember);
            }

            var memberCount = await _teamMemberRepository.CountByTeamAsync(team.Id, cancellationToken);
            if (1 + memberCount >= team.MemberLimit) {
                return new JoinResult(await ToSummaryAsync(team, userId, cancellationToken), JoinOutcome.TeamFull);
            }

            await _teamMemberRepository.AddAsync(new TeamMember {
                Id       = Guid.NewGuid(),
                TeamId   = team.Id,
                UserId   = userId,
                JoinedAt = _clock.UtcNow
            }, cancellationToken);

            return new JoinResult(await ToSummaryAsync(team, userId, cancellationToken), JoinOutcome.Joined);
        }

        public async Task LeaveAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default) {
            var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken);
            if (team is null) {
                throw new NotFoundException("Time não encontrado.");
            }

            // dono não pode sair enquanto o time não tiver outro responsável
            if (team.OwnerId == userId) {
                throw new InvalidTeamException("Transfira a titularidade ou exclua o time antes de sair.");
            }

            var member = await _teamMemberRepository.GetAsync(teamId, userId, cancellationToken);
            if (member is null) {
                throw new NotFoundException("Você não é membro desse time.");
            }

            await _teamMemberRepository.RemoveAsync(member, cancellationToken);
        }

        public async Task RemoveMemberAsync(Guid ownerId, Guid teamId, Guid memberUserId, CancellationToken cancellationToken = default) {
            await GetOwnedTeamAsync(ownerId, teamId, cancellationToken);

            if (memberUserId == ownerId) {
                throw new InvalidTeamException("O dono não pode remover a si mesmo.");
            }

            var member = await _teamMemberRepository.GetAsync(teamId, memberUserId, cancellationToken);
            if (member is null) {
                throw new NotFoundException("Esse usuário não é membro do time.");
            }

            await _teamMemberRepository.RemoveAsync(member, cancellationToken);
        }

        public async Task DeleteTeamAsync(Guid ownerId, Guid teamId, CancellationToken cancellationToken = default) {
            var team = await GetOwnedTeamAsync(ownerId, teamId, cancellationToken);

            await _teamMemberRepository.RemoveAllForTeamAsync(teamId, cancellationToken);
            await _teamInviteRepository.RemoveAllForTeamAsync(teamId, cancellationToken);
            await _teamRepository.DeleteAsync(team, cancellationToken);
        }

        public async Task TransferOwnershipAsync(Guid currentOwnerId, Guid teamId, Guid newOwnerId, CancellationToken cancellationToken = default) {
            var team = await GetOwnedTeamAsync(currentOwnerId, teamId, cancellationToken);

            if (newOwnerId == currentOwnerId) {
                throw new InvalidTeamException("Você já é o dono desse time.");
            }

            // só pode transferir a titularidade pra quem já é membro do time
            var newOwnerMember = await _teamMemberRepository.GetAsync(teamId, newOwnerId, cancellationToken);
            if (newOwnerMember is null) {
                throw new InvalidTeamException("O novo dono precisa já ser membro do time.");
            }

            await _teamMemberRepository.RemoveAsync(newOwnerMember, cancellationToken);

            team.OwnerId   = newOwnerId;
            team.UpdatedAt = _clock.UtcNow;
            await _teamRepository.UpdateAsync(team, cancellationToken);

            // o ex-dono continua no time, agora como membro comum
            await _teamMemberRepository.AddAsync(new TeamMember {
                Id       = Guid.NewGuid(),
                TeamId   = teamId,
                UserId   = currentOwnerId,
                JoinedAt = _clock.UtcNow
            }, cancellationToken);
        }

        // internos

        private async Task<IReadOnlyList<TeamSummary>> ToSummariesAsync(IReadOnlyList<Team> teams, Guid userId, CancellationToken cancellationToken) {
            if (teams.Count == 0) {
                return Array.Empty<TeamSummary>();
            }

            var results = new List<TeamSummary>(teams.Count);
            foreach (var team in teams) {
                // dono removido (soft delete): ignora pra não quebrar a listagem toda por causa de um time órfão — mesmo critério de TournamentService
                var summary = await TryToSummaryAsync(team, userId, cancellationToken);
                if (summary is not null) {
                    results.Add(summary);
                }
            }

            return results.OrderBy(t => t.Name).ToList();
        }

        // versão que não lança: dono removido vira null em vez de exceção, pra quem lista vários times poder pular o órfão (ver ToSummariesAsync)
        private async Task<TeamSummary?> TryToSummaryAsync(Team team, Guid userId, CancellationToken cancellationToken) {
            var owner = await _userRepository.GetByIdAsync(team.OwnerId, cancellationToken);
            if (owner is null) {
                return null;
            }

            var memberCount = await _teamMemberRepository.CountByTeamAsync(team.Id, cancellationToken);
            var isOwner     = team.OwnerId == userId;
            var isMember    = isOwner || await _teamMemberRepository.ExistsAsync(team.Id, userId, cancellationToken);

            return new TeamSummary(
                team.Id,
                team.Name,
                team.Description,
                ToSummary(owner),
                1 + memberCount,
                team.MemberLimit,
                team.TotalPoints,
                isOwner,
                isMember,
                team.Visibility,
                team.BannerTheme,
                team.BannerImageUrl);
        }

        // dono removido: trata o time como inexistente — usado onde só existe um time em jogo (CreateAsync, GetDetailsAsync, JoinViaLinkAsync, ações de banner), não dá pra simplesmente pular como na listagem
        private async Task<TeamSummary> ToSummaryAsync(Team team, Guid userId, CancellationToken cancellationToken) {
            var summary = await TryToSummaryAsync(team, userId, cancellationToken);
            if (summary is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }
            return summary;
        }

        // carrega o time garantindo que o usuário tem acesso: dono, membro, ou admin (o painel
        // admin lista todos os times e precisa poder abrir o detalhe de qualquer um deles)
        private async Task<Team> GetOwnedOrMemberTeamAsync(Guid userId, Guid teamId, CancellationToken cancellationToken) {
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

        // carrega o time garantindo que o usuário é o dono
        private async Task<Team> GetOwnedTeamAsync(Guid ownerId, Guid teamId, CancellationToken cancellationToken) {
            var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken);
            if (team is null || team.OwnerId != ownerId) {
                throw new NotFoundException("Time não encontrado.");
            }

            return team;
        }

        // garante que o convite está pendente e pertence a esse usuário
        private async Task<TeamInvite> GetPendingAddressedToAsync(Guid userId, Guid inviteId, CancellationToken cancellationToken) {
            var invite = await _teamInviteRepository.GetByIdAsync(inviteId, cancellationToken);

            if (invite is null || invite.InviteeId != userId) {
                throw new NotFoundException("Convite não encontrado.");
            }

            if (invite.Status != TeamInviteStatus.Pendente) {
                throw new InvalidTeamException("Esse convite já foi respondido.");
            }

            return invite;
        }

        private async Task<Dictionary<Guid, User>> LoadUsersAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken) {
            var distinct = ids.Distinct().ToList();
            var users = await _userRepository.GetByIdsAsync(distinct, cancellationToken);
            return users.ToDictionary(u => u.Id);
        }

        private static UserSummary ToSummary(User user) => new(user.Id, user.Name, user.Username);

        // torneios ativos do time, até o limite de MaxTournamentsPerTeam
        private async Task<IReadOnlyList<ActiveTeamTournament>> GetActiveTournamentsAsync(Guid teamId, CancellationToken cancellationToken) {
            var entries = await _tournamentTeamRepository.GetActiveEntriesForTeamAsync(teamId, cancellationToken);
            if (entries.Count == 0) {
                return Array.Empty<ActiveTeamTournament>();
            }

            var result = new List<ActiveTeamTournament>(entries.Count);
            foreach (var entry in entries) {
                // torneio removido: ignora pra não quebrar a listagem
                var tournament = await _tournamentRepository.GetByIdAsync(entry.TournamentId, cancellationToken);
                if (tournament is null) {
                    continue;
                }
                result.Add(new ActiveTeamTournament(tournament.Id, tournament.Name, entry.Status));
            }

            return result;
        }
    }
}
