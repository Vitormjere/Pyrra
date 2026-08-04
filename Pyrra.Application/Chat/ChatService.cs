using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Application.Comunidade;
using Pyrra.Domain.Chat;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Chat {
    public class ChatService : IChatService {
        private const int MaxContentLength = 2000;

        private readonly IChatMessageRepository _messageRepository;
        private readonly IUserRepository        _userRepository;
        private readonly IClockService           _clock;

        public ChatService(IChatMessageRepository messageRepository, IUserRepository userRepository, IClockService clock) {
            _messageRepository = messageRepository;
            _userRepository    = userRepository;
            _clock             = clock;
        }

        public async Task<IReadOnlyList<ChatConversationSummary>> GetConversationsAsync(Guid userId, CancellationToken cancellationToken = default) {
            var messages = await _messageRepository.GetAllForUserAsync(userId, cancellationToken);
            if (messages.Count == 0) {
                return Array.Empty<ChatConversationSummary>();
            }

            var counterpartIds = messages
                .Select(m => m.SenderId == userId ? m.RecipientId : m.SenderId)
                .Distinct()
                .ToList();
            var counterparts = await LoadUsersAsync(counterpartIds, cancellationToken);

            var result = new List<ChatConversationSummary>();
            foreach (var counterpartId in counterpartIds) {
                // Contraparte com conta excluída: ignora, mesmo critério de listagens em outros
                // módulos (ex.: GetPendingRequestsAsync) — não quebra a lista por causa de UMA.
                if (!counterparts.TryGetValue(counterpartId, out var counterpart)) {
                    continue;
                }

                var conversation = messages
                    .Where(m => m.SenderId == counterpartId || m.RecipientId == counterpartId)
                    .ToList();
                var last = conversation.OrderByDescending(m => m.CreatedAt).First();
                var unread = conversation.Count(m => m.SenderId == counterpartId && m.RecipientId == userId && m.ReadAt is null);

                result.Add(new ChatConversationSummary(
                    ToSummary(counterpart), last.Content, last.CreatedAt, last.SenderId == userId, unread));
            }

            return result.OrderByDescending(c => c.LastMessageAt).ToList();
        }

        public async Task<IReadOnlyList<ChatMessageSummary>> GetConversationAsync(Guid userId, Guid counterpartId, CancellationToken cancellationToken = default) {
            var messages = await _messageRepository.GetConversationAsync(userId, counterpartId, cancellationToken);
            if (messages.Count == 0) {
                return Array.Empty<ChatMessageSummary>();
            }

            var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();
            var senders = await LoadUsersAsync(senderIds, cancellationToken);

            var result = new List<ChatMessageSummary>(messages.Count);
            foreach (var message in messages.OrderBy(m => m.CreatedAt)) {
                // Remetente com conta excluída: a mensagem já foi lida por quem manda essa
                // requisição, então continua aparecendo — só não teria como resolver Sender.Name.
                if (!senders.TryGetValue(message.SenderId, out var sender)) {
                    continue;
                }
                result.Add(ToSummary(message, ToSummary(sender)));
            }

            return result;
        }

        public async Task<ChatMessageSummary> SendMessageAsync(
            Guid senderId, Guid recipientId, string content, CancellationToken cancellationToken = default) {
            var normalizedContent = content?.Trim();
            if (string.IsNullOrEmpty(normalizedContent)) {
                throw new InvalidChatMessageException("A mensagem não pode ficar vazia.");
            }
            if (normalizedContent.Length > MaxContentLength) {
                throw new InvalidChatMessageException($"A mensagem deve ter até {MaxContentLength} caracteres.");
            }
            if (senderId == recipientId) {
                throw new InvalidChatMessageException("Não é possível enviar mensagem para si mesmo.");
            }

            var sender = await _userRepository.GetByIdAsync(senderId, cancellationToken);
            if (sender is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }
            var recipient = await _userRepository.GetByIdAsync(recipientId, cancellationToken);
            if (recipient is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }

            // Conversa é sempre entre um admin e um jogador — nunca admin-admin, nunca
            // jogador-jogador. IsAdmin igual dos dois lados quebra essa regra.
            if (sender.IsAdmin == recipient.IsAdmin) {
                throw new InvalidChatMessageException("Conversas só acontecem entre um admin e um jogador.");
            }

            var message = new ChatMessage {
                Id          = Guid.NewGuid(),
                SenderId    = senderId,
                RecipientId = recipientId,
                Content     = normalizedContent,
                CreatedAt   = _clock.UtcNow
            };
            await _messageRepository.AddAsync(message, cancellationToken);

            return ToSummary(message, ToSummary(sender));
        }

        public Task MarkConversationAsReadAsync(Guid userId, Guid counterpartId, CancellationToken cancellationToken = default) =>
            _messageRepository.MarkConversationAsReadAsync(userId, counterpartId, _clock.UtcNow, cancellationToken);

        public async Task<IReadOnlyList<UserSummary>> GetActiveAdminsAsync(CancellationToken cancellationToken = default) {
            // catálogo pequeno de admins, busca completa em memória simplifica (mesmo critério do
            // resto do projeto) — GetAllAsync já existe desde a Fase Admin-2/2.1.
            var all = await _userRepository.GetAllAsync(cancellationToken);
            return all
                .Where(u => u.IsAdmin && u.DeletedAt is null)
                .OrderBy(u => u.Name)
                .Select(ToSummary)
                .ToList();
        }

        private async Task<Dictionary<Guid, User>> LoadUsersAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken) {
            var distinct = ids.Distinct().ToList();
            var users = await _userRepository.GetByIdsAsync(distinct, cancellationToken);
            return users.ToDictionary(u => u.Id);
        }

        private static UserSummary ToSummary(User user) => new(user.Id, user.Name, user.Username);

        private static ChatMessageSummary ToSummary(ChatMessage message, UserSummary sender) => new(
            message.Id, sender, message.RecipientId, message.Content, message.CreatedAt, message.ReadAt);
    }
}
