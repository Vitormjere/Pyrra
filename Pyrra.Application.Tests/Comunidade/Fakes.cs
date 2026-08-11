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

namespace Pyrra.Application.Tests.Comunidade {
    // relógio fixo: torna CreatedAt/RespondedAt determinísticos nos testes
    internal sealed class FakeClock : IClockService {
        public DateTime UtcNow { get; set; } = new(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);
        public DateOnly TodayIn(string timezoneId) => DateOnly.FromDateTime(UtcNow);
        public DateOnly ToLocalDate(DateTime utc, string timezoneId) => DateOnly.FromDateTime(utc);
    }

    // só conta chamadas e devolve uma URL fake, pra testar tipo/tamanho/prioridade sem Blob Storage real
    internal sealed class FakeTeamBannerStorageService : ITeamBannerStorageService {
        public int UploadCallCount { get; private set; }
        public int DeleteCallCount { get; private set; }

        public Task<string> UploadAsync(Guid teamId, Stream content, string contentType, CancellationToken cancellationToken = default) {
            UploadCallCount++;
            return Task.FromResult($"https://fake.blob.core.windows.net/team-banners/{teamId:N}");
        }

        public Task DeleteAsync(Guid teamId, CancellationToken cancellationToken = default) {
            DeleteCallCount++;
            return Task.CompletedTask;
        }
    }

    // mesmo padrão do FakeTeamBannerStorageService acima, agora pra foto de perfil de usuário
    internal sealed class FakeUserProfilePictureStorageService : IUserProfilePictureStorageService {
        public int UploadCallCount { get; private set; }
        public int DeleteCallCount { get; private set; }

        public Task<string> UploadAsync(Guid userId, Stream content, string contentType, CancellationToken cancellationToken = default) {
            UploadCallCount++;
            return Task.FromResult($"https://fake.blob.core.windows.net/profile-pictures/{userId:N}");
        }

        public Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default) {
            DeleteCallCount++;
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeUserRepository : IUserRepository {
        public readonly List<User> Users = new();

        public FakeUserRepository(params User[] users) => Users.AddRange(users);

        // mesmo filtro do UserRepository real: usuário com DeletedAt marcado não existe pra nenhuma consulta
        private IEnumerable<User> Active => Users.Where(u => u.DeletedAt is null);

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Active.FirstOrDefault(u => u.Email == email));

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Active.FirstOrDefault(u => u.Id == id));

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
            Task.FromResult(Active.FirstOrDefault(u => u.Username == username));

        // sem o filtro de Active de propósito — mesmo critério do UserRepository real
        public Task<User?> GetByUsernameIncludingDeletedAsync(string username, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.FirstOrDefault(u => u.Username == username));

        public Task<User?> GetByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken = default) =>
            Task.FromResult(Active.FirstOrDefault(u => u.InviteToken == inviteToken));

        public Task<IReadOnlyList<User>> SearchAsync(string term, Guid excludeUserId, CancellationToken cancellationToken = default) {
            var normalized = term.Trim().TrimStart('@').ToLowerInvariant();
            var result = Active
                .Where(u => u.Id != excludeUserId
                    && u.Username != null
                    && (u.Username.Contains(normalized) || u.Email.Contains(normalized)))
                .ToList();
            return Task.FromResult<IReadOnlyList<User>>(result);
        }

        public Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<User>>(Active.Where(u => ids.Contains(u.Id)).ToList());

        // diferente dos demais, não filtra por Active — igual ao UserRepository real
        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<User>>(Users.OrderBy(u => u.Name).ToList());

        public Task AddAsync(User user, CancellationToken cancellationToken = default) {
            // mesma proteção de corrida do UpdateAsync abaixo — simula os índices únicos do UserRepository real, agora também no insert
            // contra Users, não Active: os índices reais não liberam username/email de conta excluída
            if (user.Username is not null && Users.Any(u => u.Id != user.Id && u.Username == user.Username)) {
                throw new UsernameAlreadyTakenException(user.Username);
            }
            if (Users.Any(u => u.Id != user.Id && u.Email == user.Email)) {
                throw new EmailAlreadyRegisteredException(user.Email);
            }
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) {
            // simula os índices únicos de username e e-mail — contra Users, não Active, mesmo critério do AddAsync acima
            if (user.Username is not null && Users.Any(u => u.Id != user.Id && u.Username == user.Username)) {
                throw new UsernameAlreadyTakenException(user.Username);
            }
            if (Users.Any(u => u.Id != user.Id && u.Email == user.Email)) {
                throw new EmailAlreadyRegisteredException(user.Email);
            }
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeFriendshipRepository : IFriendshipRepository {
        public readonly List<Friendship> Friendships = new();

        public Task<Friendship?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Friendships.FirstOrDefault(f => f.Id == id));

        public Task<Friendship?> GetBetweenAsync(Guid userA, Guid userB, CancellationToken cancellationToken = default) =>
            Task.FromResult(Friendships.FirstOrDefault(f =>
                (f.RequesterId == userA && f.AddresseeId == userB) ||
                (f.RequesterId == userB && f.AddresseeId == userA)));

        public Task<IReadOnlyList<Friendship>> GetAcceptedForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Friendship>>(Friendships
                .Where(f => f.Status == FriendshipStatus.Aceito && (f.RequesterId == userId || f.AddresseeId == userId))
                .ToList());

        public Task<IReadOnlyList<Friendship>> GetPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Friendship>>(Friendships
                .Where(f => f.Status == FriendshipStatus.Pendente && f.AddresseeId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList());

        public Task<int> CountPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Friendships.Count(f => f.Status == FriendshipStatus.Pendente && f.AddresseeId == userId));

        public Task<int> CountAcceptedForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Friendships.Count(f =>
                f.Status == FriendshipStatus.Aceito && (f.RequesterId == userId || f.AddresseeId == userId)));

        public Task AddAsync(Friendship friendship, CancellationToken cancellationToken = default) {
            Friendships.Add(friendship);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Friendship friendship, CancellationToken cancellationToken = default) {
            // a instância já está na lista (fake devolve a referência real), nada a fazer
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Friendship friendship, CancellationToken cancellationToken = default) {
            Friendships.RemoveAll(f => f.Id == friendship.Id);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeTeamRepository : ITeamRepository {
        public readonly List<Team> Teams = new();

        private readonly FakeTeamMemberRepository _members;

        public FakeTeamRepository(FakeTeamMemberRepository members) {
            _members = members;
        }

        public Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Teams.FirstOrDefault(t => t.Id == id));

        public Task<Team?> GetByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken = default) =>
            Task.FromResult(Teams.FirstOrDefault(t => t.InviteToken == inviteToken));

        public Task<IReadOnlyList<Team>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Team>>(Teams
                .Where(t => t.OwnerId == userId || _members.Members.Any(m => m.TeamId == t.Id && m.UserId == userId))
                .ToList());

        public Task<IReadOnlyList<Team>> GetPublicAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Team>>(Teams
                .Where(t => t.Visibility == TeamVisibility.Publico)
                .OrderBy(t => t.Name)
                .ToList());

        public Task<IReadOnlyList<Team>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Team>>(Teams.OrderBy(t => t.Name).ToList());

        public Task AddAsync(Team team, CancellationToken cancellationToken = default) {
            Teams.Add(team);
            return Task.CompletedTask;
        }

        public void AddMember(TeamMember member) => _members.Members.Add(member);

        public Task UpdateAsync(Team team, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(Team team, CancellationToken cancellationToken = default) {
            Teams.RemoveAll(t => t.Id == team.Id);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeTeamMemberRepository : ITeamMemberRepository {
        public readonly List<TeamMember> Members = new();

        public Task<TeamMember?> GetAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Members.FirstOrDefault(m => m.TeamId == teamId && m.UserId == userId));

        public Task<IReadOnlyList<TeamMember>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TeamMember>>(Members.Where(m => m.TeamId == teamId).ToList());

        public Task<int> CountByTeamAsync(Guid teamId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Members.Count(m => m.TeamId == teamId));

        public Task<bool> ExistsAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Members.Any(m => m.TeamId == teamId && m.UserId == userId));

        public Task AddAsync(TeamMember member, CancellationToken cancellationToken = default) {
            Members.Add(member);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(TeamMember member, CancellationToken cancellationToken = default) {
            Members.RemoveAll(m => m.Id == member.Id);
            return Task.CompletedTask;
        }

        public Task RemoveAllForTeamAsync(Guid teamId, CancellationToken cancellationToken = default) {
            Members.RemoveAll(m => m.TeamId == teamId);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeTeamInviteRepository : ITeamInviteRepository {
        public readonly List<TeamInvite> Invites = new();

        public Task<TeamInvite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Invites.FirstOrDefault(i => i.Id == id));

        public Task<TeamInvite?> GetByTeamAndInviteeAsync(Guid teamId, Guid inviteeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Invites.FirstOrDefault(i => i.TeamId == teamId && i.InviteeId == inviteeId));

        public Task<IReadOnlyList<TeamInvite>> GetPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TeamInvite>>(Invites
                .Where(i => i.Status == TeamInviteStatus.Pendente && i.InviteeId == userId)
                .OrderByDescending(i => i.CreatedAt)
                .ToList());

        public Task<int> CountPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Invites.Count(i => i.Status == TeamInviteStatus.Pendente && i.InviteeId == userId));

        public Task AddAsync(TeamInvite invite, CancellationToken cancellationToken = default) {
            Invites.Add(invite);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(TeamInvite invite, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RemoveAllForTeamAsync(Guid teamId, CancellationToken cancellationToken = default) {
            Invites.RemoveAll(i => i.TeamId == teamId);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeTournamentRepository : ITournamentRepository {
        public readonly List<Tournament> Tournaments = new();

        public Task<Tournament?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Tournaments.FirstOrDefault(t => t.Id == id));

        public Task<Tournament?> GetByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken = default) =>
            Task.FromResult(Tournaments.FirstOrDefault(t => t.InviteToken == inviteToken));

        public Task<IReadOnlyList<Tournament>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Tournament>>(Tournaments.OrderBy(t => t.Name).ToList());

        public Task<IReadOnlyList<Tournament>> GetOwnedByUserAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Tournament>>(Tournaments.Where(t => t.OwnerId == ownerId).ToList());

        public Task AddAsync(Tournament tournament, CancellationToken cancellationToken = default) {
            Tournaments.Add(tournament);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Tournament tournament, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    internal sealed class FakeTournamentRequestRepository : ITournamentRequestRepository {
        public readonly List<TournamentRequest> Requests = new();

        public Task<TournamentRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Requests.FirstOrDefault(r => r.Id == id));

        public Task<IReadOnlyList<TournamentRequest>> GetPendingAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TournamentRequest>>(
                Requests.Where(r => r.Status == TournamentRequestStatus.Pendente).ToList());

        public Task<IReadOnlyList<TournamentRequest>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TournamentRequest>>(Requests.ToList());

        public Task AddAsync(TournamentRequest request, CancellationToken cancellationToken = default) {
            Requests.Add(request);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(TournamentRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    // só conta chamadas e devolve uma URL fake
    internal sealed class FakeTournamentBannerStorageService : ITournamentBannerStorageService {
        public int UploadCallCount { get; private set; }
        public int DeleteCallCount { get; private set; }

        public Task<string> UploadAsync(Guid tournamentId, Stream content, string contentType, CancellationToken cancellationToken = default) {
            UploadCallCount++;
            return Task.FromResult($"https://fake.blob.core.windows.net/tournament-banners/{tournamentId:N}");
        }

        public Task DeleteAsync(Guid tournamentId, CancellationToken cancellationToken = default) {
            DeleteCallCount++;
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeTournamentTeamRepository : ITournamentTeamRepository {
        public readonly List<TournamentTeam> Entries = new();

        public Task<TournamentTeam?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Entries.FirstOrDefault(e => e.Id == id));

        public Task<IReadOnlyList<TournamentTeam>> GetActiveEntriesForTeamAsync(Guid teamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TournamentTeam>>(
                Entries.Where(e => e.TeamId == teamId && e.Status != TournamentTeamStatus.Recusado).ToList());

        public Task<IReadOnlyList<TournamentTeam>> GetPendingForTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TournamentTeam>>(
                Entries.Where(e => e.TournamentId == tournamentId && e.Status == TournamentTeamStatus.Pendente).ToList());

        public Task<IReadOnlyList<TournamentTeam>> GetApprovedForTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TournamentTeam>>(
                Entries.Where(e => e.TournamentId == tournamentId && e.Status == TournamentTeamStatus.Aprovado).ToList());

        public Task AddAsync(TournamentTeam tournamentTeam, CancellationToken cancellationToken = default) {
            Entries.Add(tournamentTeam);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(TournamentTeam tournamentTeam, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
