using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Chat;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class ChatMessageRepository : IChatMessageRepository {
        private readonly PyrraDbContext _context;

        public ChatMessageRepository(PyrraDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<ChatMessage>> GetAllForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            await _context.ChatMessages
                .Where(m => m.SenderId == userId || m.RecipientId == userId)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<ChatMessage>> GetConversationAsync(Guid userId, Guid counterpartId, CancellationToken cancellationToken = default) =>
            await _context.ChatMessages
                .Where(m =>
                    (m.SenderId == userId && m.RecipientId == counterpartId) ||
                    (m.SenderId == counterpartId && m.RecipientId == userId))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync(cancellationToken);

        public async Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default) {
            await _context.ChatMessages.AddAsync(message, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkConversationAsReadAsync(Guid userId, Guid counterpartId, DateTime readAt, CancellationToken cancellationToken = default) {
            var unread = await _context.ChatMessages
                .Where(m => m.SenderId == counterpartId && m.RecipientId == userId && m.ReadAt == null)
                .ToListAsync(cancellationToken);

            if (unread.Count == 0) {
                return;
            }

            foreach (var message in unread) {
                message.ReadAt = readAt;
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
