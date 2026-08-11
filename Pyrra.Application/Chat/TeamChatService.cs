using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Application.Comunidade;
using Pyrra.Domain.Chat;
using Pyrra.Domain.Comunidade;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Chat {
    public class TeamChatService : ITeamChatService {
        private const int MaxContentLength = 2000;

        private readonly ITeamChatMessageRepository _messageRepository;
        private readonly ITeamRepository             _teamRepository;
        private readonly ITeamMemberRepository       _teamMemberRepository;
        private readonly IUserRepository             _userRepository;
        private readonly IClockService                _clock;

        public TeamChatService(
            ITeamChatMessageRepository messageRepository,
            ITeamRepository teamRepository,
            ITeamMemberRepository teamMemberRepository,
            IUserRepository userRepository,
            IClockService clock) {
            _messageRepository    = messageRepository;
            _teamRepository       = teamRepository;
            _teamMemberRepository = teamMemberRepository;
            _userRepository       = userRepository;
            _clock                = clock;
        }

        public async Task<IReadOnlyList<TeamChatMessageSummary>> GetMessagesAsync(
            Guid userId, Guid teamId, CancellationToken cancellationToken = default) {
            await EnsureMemberAsync(userId, teamId, cancellationToken);

            var messages = await _messageRepository.GetByTeamAsync(teamId, cancellationToken);
            if (messages.Count == 0) {
                return Array.Empty<TeamChatMessageSummary>();
            }

            var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();
            var senders = await LoadUsersAsync(senderIds, cancellationToken);

            var result = new List<TeamChatMessageSummary>(messages.Count);
            foreach (var message in messages) {
                // mesmo critério do chat 1-a-1: remetente com conta excluída não aparece
                if (!senders.TryGetValue(message.SenderId, out var sender)) {
                    continue;
                }
                result.Add(ToSummary(message, ToSummary(sender)));
            }

            return result;
        }

        public async Task<TeamChatSendResult> SendMessageAsync(
            Guid userId, Guid teamId, string content, CancellationToken cancellationToken = default) {
            var normalizedContent = content?.Trim();
            if (string.IsNullOrEmpty(normalizedContent)) {
                throw new InvalidChatMessageException("A mensagem não pode ficar vazia.");
            }
            if (normalizedContent.Length > MaxContentLength) {
                throw new InvalidChatMessageException($"A mensagem deve ter até {MaxContentLength} caracteres.");
            }

            var team = await EnsureMemberAsync(userId, teamId, cancellationToken);

            var sender = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (sender is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }

            var message = new TeamChatMessage {
                Id        = Guid.NewGuid(),
                TeamId    = teamId,
                SenderId  = userId,
                Content   = normalizedContent,
                CreatedAt = _clock.UtcNow
            };
            await _messageRepository.AddAsync(message, cancellationToken);

            var members = await _teamMemberRepository.GetByTeamAsync(teamId, cancellationToken);
            var recipientIds = new List<Guid>();
            if (team.OwnerId != userId) {
                recipientIds.Add(team.OwnerId);
            }
            recipientIds.AddRange(members.Select(m => m.UserId).Where(id => id != userId));

            return new TeamChatSendResult(ToSummary(message, ToSummary(sender)), recipientIds);
        }

        // garante que o usuário é dono ou membro do time — sem exceção pra admin, o chat de time é privado mesmo pra quem administra o app
        private async Task<Team> EnsureMemberAsync(Guid userId, Guid teamId, CancellationToken cancellationToken) {
            var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken);
            if (team is null) {
                throw new NotFoundException("Time não encontrado.");
            }

            var isMember = team.OwnerId == userId || await _teamMemberRepository.ExistsAsync(teamId, userId, cancellationToken);
            if (!isMember) {
                throw new NotFoundException("Time não encontrado.");
            }

            return team;
        }

        private async Task<Dictionary<Guid, User>> LoadUsersAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken) {
            var distinct = ids.Distinct().ToList();
            var users = await _userRepository.GetByIdsAsync(distinct, cancellationToken);
            return users.ToDictionary(u => u.Id);
        }

        private static UserSummary ToSummary(User user) => new(user.Id, user.Name, user.Username);

        private static TeamChatMessageSummary ToSummary(TeamChatMessage message, UserSummary sender) => new(
            message.Id, sender, message.TeamId, message.Content, message.CreatedAt);
    }
}
