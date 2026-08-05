using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Chat;

namespace Pyrra.Application.Tests.Chat {
    internal sealed class FakeChatMessageRepository : IChatMessageRepository {
        public readonly List<ChatMessage> Messages = new();

        public Task<IReadOnlyList<ChatMessage>> GetAllForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChatMessage>>(
                Messages.Where(m => m.SenderId == userId || m.RecipientId == userId).ToList());

        public Task<IReadOnlyList<ChatMessage>> GetConversationAsync(Guid userId, Guid counterpartId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChatMessage>>(
                Messages
                    .Where(m =>
                        (m.SenderId == userId && m.RecipientId == counterpartId) ||
                        (m.SenderId == counterpartId && m.RecipientId == userId))
                    .OrderBy(m => m.CreatedAt)
                    .ToList());

        public Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default) {
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public Task MarkConversationAsReadAsync(Guid userId, Guid counterpartId, DateTime readAt, CancellationToken cancellationToken = default) {
            foreach (var message in Messages.Where(m => m.SenderId == counterpartId && m.RecipientId == userId && m.ReadAt is null)) {
                message.ReadAt = readAt;
            }
            return Task.CompletedTask;
        }
    }
}
