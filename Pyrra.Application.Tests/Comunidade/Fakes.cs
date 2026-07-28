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
    // Relógio fixo: torna CreatedAt/RespondedAt determinísticos nos testes.
    internal sealed class FakeClock : IClockService {
        public DateTime UtcNow { get; set; } = new(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);
        public DateOnly TodayIn(string timezoneId) => DateOnly.FromDateTime(UtcNow);
        public DateOnly ToLocalDate(DateTime utc, string timezoneId) => DateOnly.FromDateTime(utc);
    }

    // Só conta chamadas e devolve uma URL fake determinística — os testes de TeamService validam
    // a lógica de tipo/tamanho/prioridade sem precisar de um Blob Storage real.
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

    internal sealed class FakeUserRepository : IUserRepository {
        public readonly List<User> Users = new();

        public FakeUserRepository(params User[] users) => Users.AddRange(users);

        // Mesmo filtro do UserRepository real: um usuário com DeletedAt marcado simplesmente não
        // existe para nenhuma consulta — é isso que os testes de exclusão de conta verificam.
        private IEnumerable<User> Active => Users.Where(u => u.DeletedAt is null);

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Active.FirstOrDefault(u => u.Email == email));

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Active.FirstOrDefault(u => u.Id == id));

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
            Task.FromResult(Active.FirstOrDefault(u => u.Username == username));

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

        public Task AddAsync(User user, CancellationToken cancellationToken = default) {
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) {
            // Simula os índices únicos de username e e-mail: se outro usuário ativo já tem o
            // mesmo valor, é violação — mesmo critério do UserRepository real.
            if (user.Username is not null && Active.Any(u => u.Id != user.Id && u.Username == user.Username)) {
                throw new UsernameAlreadyTakenException(user.Username);
            }
            if (Active.Any(u => u.Id != user.Id && u.Email == user.Email)) {
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
            // A instância já está na lista (fake devolve a referência real), nada a fazer.
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Friendship friendship, CancellationToken cancellationToken = default) {
            Friendships.RemoveAll(f => f.Id == friendship.Id);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeTeamRepository : ITeamRepository {
        public readonly List<Team> Teams = new();

        // Referência à mesma lista usada pelo FakeTeamMemberRepository — GetForUserAsync precisa
        // combinar "sou dono" com "tenho uma linha de membership", igual à query real do
        // TeamRepository (Any() sobre TeamMembers, sem navigation property).
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

        public Task AddAsync(Team team, CancellationToken cancellationToken = default) {
            Teams.Add(team);
            return Task.CompletedTask;
        }

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
}
