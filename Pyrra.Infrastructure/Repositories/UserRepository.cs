using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Users;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class UserRepository : IUserRepository {
     
        private const int SearchLimit = 20;

        private readonly PyrraDbContext _context;

        public UserRepository(PyrraDbContext context) {
            _context = context;
        }

        private IQueryable<User> ActiveUsers => _context.Users.Where(u => u.DeletedAt == null);

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            ActiveUsers.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            ActiveUsers.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
            ActiveUsers.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        public Task<User?> GetByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken = default) =>
            ActiveUsers.FirstOrDefaultAsync(u => u.InviteToken == inviteToken, cancellationToken);

        public async Task<IReadOnlyList<User>> SearchAsync(string term, Guid excludeUserId, CancellationToken cancellationToken = default) {
            var normalized = term.Trim().TrimStart('@').ToLowerInvariant();
            if (normalized.Length == 0) {
                return Array.Empty<User>();
            }

            return await ActiveUsers
                .Where(u => u.Id != excludeUserId
                    && u.Username != null
                    && (u.Username.Contains(normalized) || u.Email.Contains(normalized)))
                .OrderBy(u => u.Username)
                .Take(SearchLimit)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) {
            if (ids.Count == 0) {
                return Array.Empty<User>();
            }

            return await ActiveUsers
                .Where(u => ids.Contains(u.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _context.Users.OrderBy(u => u.Name).ToListAsync(cancellationToken);

        public async Task AddAsync(User user, CancellationToken cancellationToken = default) {
            await _context.Users.AddAsync(user, cancellationToken);
            try {
                await _context.SaveChangesAsync(cancellationToken);
            } catch (DbUpdateException ex) when (IsUniqueViolation(ex, "IX_Users_Username")) {
                throw new UsernameAlreadyTakenException(user.Username ?? string.Empty);
            } catch (DbUpdateException ex) when (IsUniqueViolation(ex, "IX_Users_Email")) {
                throw new EmailAlreadyRegisteredException(user.Email);
            }
        }

        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default) {
            _context.Users.Update(user);
            try {
                await _context.SaveChangesAsync(cancellationToken);
            } catch (DbUpdateException ex) when (IsUniqueViolation(ex, "IX_Users_Username")) {
                throw new UsernameAlreadyTakenException(user.Username ?? string.Empty);
            } catch (DbUpdateException ex) when (IsUniqueViolation(ex, "IX_Users_Email")) {
                throw new EmailAlreadyRegisteredException(user.Email);
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex, string indexName) {
            var message = ex.InnerException?.Message ?? ex.Message;
            return message.Contains(indexName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
