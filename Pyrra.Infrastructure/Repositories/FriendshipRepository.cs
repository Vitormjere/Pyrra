using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Comunidade;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class FriendshipRepository : IFriendshipRepository {
        private readonly PyrraDbContext _context;

        public FriendshipRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<Friendship?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.Friendships.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        // Nos dois sentidos: o par (A,B) pode estar gravado como A→B ou B→A. Uma linha por par é
        // invariante garantida pelo service, então FirstOrDefault basta.
        public Task<Friendship?> GetBetweenAsync(Guid userA, Guid userB, CancellationToken cancellationToken = default) =>
            _context.Friendships.FirstOrDefaultAsync(
                f => (f.RequesterId == userA && f.AddresseeId == userB)
                  || (f.RequesterId == userB && f.AddresseeId == userA),
                cancellationToken);

        public async Task<IReadOnlyList<Friendship>> GetAcceptedForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            await _context.Friendships
                .Where(f => f.Status == FriendshipStatus.Aceito
                    && (f.RequesterId == userId || f.AddresseeId == userId))
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<Friendship>> GetPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default) =>
            await _context.Friendships
                .Where(f => f.Status == FriendshipStatus.Pendente && f.AddresseeId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync(cancellationToken);

        public Task<int> CountPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default) =>
            _context.Friendships.CountAsync(
                f => f.Status == FriendshipStatus.Pendente && f.AddresseeId == userId,
                cancellationToken);

        public Task<int> CountAcceptedForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            _context.Friendships.CountAsync(
                f => f.Status == FriendshipStatus.Aceito && (f.RequesterId == userId || f.AddresseeId == userId),
                cancellationToken);

        public async Task AddAsync(Friendship friendship, CancellationToken cancellationToken = default) {
            await _context.Friendships.AddAsync(friendship, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Friendship friendship, CancellationToken cancellationToken = default) {
            _context.Friendships.Update(friendship);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Friendship friendship, CancellationToken cancellationToken = default) {
            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
