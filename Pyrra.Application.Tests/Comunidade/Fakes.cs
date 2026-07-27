using System;
using System.Collections.Generic;
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
}
