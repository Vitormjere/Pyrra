using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Comunidade;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Comunidade {
    public class FriendshipService : IFriendshipService {
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IUserRepository       _userRepository;
        private readonly IClockService         _clock;

        public FriendshipService(
            IFriendshipRepository friendshipRepository,
            IUserRepository       userRepository,
            IClockService         clock) {
            _friendshipRepository = friendshipRepository;
            _userRepository       = userRepository;
            _clock                = clock;
        }

        public async Task<IReadOnlyList<UserSearchResult>> SearchUsersAsync(Guid userId, string term, CancellationToken cancellationToken = default) {
            var normalized = term?.Trim() ?? string.Empty;
            if (normalized.Length == 0) {
                return Array.Empty<UserSearchResult>();
            }

            var users = await _userRepository.SearchAsync(normalized, userId, cancellationToken);

            var results = new List<UserSearchResult>(users.Count);
            foreach (var user in users) {
                var existing = await _friendshipRepository.GetBetweenAsync(userId, user.Id, cancellationToken);
                results.Add(new UserSearchResult(ToSummary(user), StateFor(existing, userId)));
            }
            return results;
        }

        public async Task SendRequestAsync(Guid requesterId, Guid addresseeId, CancellationToken cancellationToken = default) {
            // Destinatário precisa existir. Vem de um resultado de busca, mas o cliente poderia mandar
            // qualquer id — a checagem fecha essa brecha.
            var addressee = await _userRepository.GetByIdAsync(addresseeId, cancellationToken);
            if (addressee is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }

            var outcome = await TrySendRequestAsync(requesterId, addresseeId, cancellationToken);
            switch (outcome) {
                case SendOutcome.Created:
                    return;
                case SendOutcome.Self:
                    throw new InvalidFriendshipException("Você não pode adicionar a si mesmo.");
                case SendOutcome.AlreadyFriends:
                    throw new InvalidFriendshipException("Vocês já são amigos.");
                case SendOutcome.AlreadyPendingOutgoing:
                    throw new InvalidFriendshipException("Você já enviou um pedido para esse usuário.");
                case SendOutcome.AlreadyPendingIncoming:
                    throw new InvalidFriendshipException("Esse usuário já te enviou um pedido — aceite-o na aba Pedidos.");
                default:
                    throw new InvalidFriendshipException("Não foi possível enviar o pedido.");
            }
        }

        public async Task<IReadOnlyList<FriendRequestSummary>> GetPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default) {
            var pending = await _friendshipRepository.GetPendingReceivedAsync(userId, cancellationToken);
            if (pending.Count == 0) {
                return Array.Empty<FriendRequestSummary>();
            }

            var requesters = await LoadUsersAsync(pending.Select(f => f.RequesterId), cancellationToken);

            return pending
                .Where(f => requesters.ContainsKey(f.RequesterId))
                .Select(f => new FriendRequestSummary(f.Id, ToSummary(requesters[f.RequesterId]), f.CreatedAt))
                .ToList();
        }

        public Task<int> GetPendingReceivedCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
            _friendshipRepository.CountPendingReceivedAsync(userId, cancellationToken);

        public async Task AcceptAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default) {
            var friendship = await GetPendingAddressedToAsync(userId, friendshipId, cancellationToken);
            friendship.Status      = FriendshipStatus.Aceito;
            friendship.RespondedAt = _clock.UtcNow;
            await _friendshipRepository.UpdateAsync(friendship, cancellationToken);
        }

        public async Task DeclineAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default) {
            var friendship = await GetPendingAddressedToAsync(userId, friendshipId, cancellationToken);
            friendship.Status      = FriendshipStatus.Recusado;
            friendship.RespondedAt = _clock.UtcNow;
            await _friendshipRepository.UpdateAsync(friendship, cancellationToken);
        }

        public async Task<IReadOnlyList<FriendSummary>> GetFriendsAsync(Guid userId, CancellationToken cancellationToken = default) {
            var accepted = await _friendshipRepository.GetAcceptedForUserAsync(userId, cancellationToken);
            if (accepted.Count == 0) {
                return Array.Empty<FriendSummary>();
            }

            // O amigo é o OUTRO lado do vínculo — requester ou addressee conforme quem enviou.
            var otherIds = accepted.Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId);
            var users = await LoadUsersAsync(otherIds, cancellationToken);

            return accepted
                .Select(f => {
                    var otherId = f.RequesterId == userId ? f.AddresseeId : f.RequesterId;
                    return users.TryGetValue(otherId, out var user)
                        ? new FriendSummary(f.Id, ToSummary(user), f.RespondedAt ?? f.CreatedAt)
                        : null;
                })
                .Where(s => s is not null)
                .Select(s => s!)
                .OrderBy(s => s.User.Name)
                .ToList();
        }

        public Task<int> GetFriendsCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
            _friendshipRepository.CountAcceptedForUserAsync(userId, cancellationToken);

        public async Task RemoveAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default) {
            var friendship = await _friendshipRepository.GetByIdAsync(friendshipId, cancellationToken);

            // Inexistente ou de terceiros são indistinguíveis para quem chama, mesmo NotFound genérico
            // dos outros módulos — não revela vínculos que não são do usuário.
            if (friendship is null || (friendship.RequesterId != userId && friendship.AddresseeId != userId)) {
                throw new NotFoundException("Amizade não encontrada.");
            }

            await _friendshipRepository.DeleteAsync(friendship, cancellationToken);
        }

        public async Task<string> GetOrCreateInviteTokenAsync(Guid userId, CancellationToken cancellationToken = default) {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }

            if (string.IsNullOrEmpty(user.InviteToken)) {
                // 32 hex (122 bits de entropia): não dá para adivinhar e não expõe o UserId. Estável
                // depois de criado, para o link compartilhado nunca deixar de valer.
                user.InviteToken = Guid.NewGuid().ToString("N");
                user.UpdatedAt   = _clock.UtcNow;
                await _userRepository.UpdateAsync(user, cancellationToken);
            }

            return user.InviteToken;
        }

        public async Task<InviteResult> AcceptInviteAsync(Guid userId, string inviteToken, CancellationToken cancellationToken = default) {
            var owner = await _userRepository.GetByInviteTokenAsync(inviteToken, cancellationToken);
            if (owner is null) {
                throw new NotFoundException("Convite inválido ou expirado.");
            }

            // O pedido vai do dono do link (owner) para... não: quem abre o link ENVIA o pedido para o
            // dono. Assim o dono, que compartilhou o link, recebe o pedido para aceitar — combina com
            // "o link já envia o pedido para quem gerou o link".
            var outcome = await TrySendRequestAsync(userId, owner.Id, cancellationToken);

            var mapped = outcome switch {
                SendOutcome.Created                 => InviteOutcome.RequestSent,
                SendOutcome.AlreadyFriends          => InviteOutcome.AlreadyFriends,
                SendOutcome.AlreadyPendingOutgoing  => InviteOutcome.AlreadyPending,
                SendOutcome.AlreadyPendingIncoming  => InviteOutcome.AlreadyPending,
                SendOutcome.Self                    => InviteOutcome.OwnLink,
                _                                   => InviteOutcome.AlreadyPending
            };

            return new InviteResult(ToSummary(owner), mapped);
        }

        // ---- internos ----

        private enum SendOutcome {
            Created,
            Self,
            AlreadyFriends,
            AlreadyPendingOutgoing,
            AlreadyPendingIncoming
        }

        // Núcleo compartilhado por SendRequest e AcceptInvite. Devolve o desfecho em vez de lançar:
        // quem chama decide se um duplicado é erro (busca) ou informação (link idempotente).
        private async Task<SendOutcome> TrySendRequestAsync(Guid requesterId, Guid addresseeId, CancellationToken cancellationToken) {
            if (requesterId == addresseeId) {
                return SendOutcome.Self;
            }

            var existing = await _friendshipRepository.GetBetweenAsync(requesterId, addresseeId, cancellationToken);

            if (existing is not null) {
                switch (existing.Status) {
                    case FriendshipStatus.Aceito:
                        return SendOutcome.AlreadyFriends;
                    case FriendshipStatus.Pendente:
                        return existing.RequesterId == requesterId
                            ? SendOutcome.AlreadyPendingOutgoing
                            : SendOutcome.AlreadyPendingIncoming;
                    case FriendshipStatus.Recusado:
                        // Recusado no passado não tranca para sempre: reaproveita a linha como um novo
                        // pedido, agora na direção de quem está pedindo.
                        existing.RequesterId = requesterId;
                        existing.AddresseeId = addresseeId;
                        existing.Status      = FriendshipStatus.Pendente;
                        existing.CreatedAt   = _clock.UtcNow;
                        existing.RespondedAt = null;
                        await _friendshipRepository.UpdateAsync(existing, cancellationToken);
                        return SendOutcome.Created;
                }
            }

            await _friendshipRepository.AddAsync(new Friendship {
                Id          = Guid.NewGuid(),
                RequesterId = requesterId,
                AddresseeId = addresseeId,
                Status      = FriendshipStatus.Pendente,
                CreatedAt   = _clock.UtcNow
            }, cancellationToken);

            return SendOutcome.Created;
        }

        // Carrega um pedido garantindo que ele é PENDENTE e endereçado ao usuário — a guarda que só
        // deixa o destinatário responder e barra aceitar/recusar duas vezes.
        private async Task<Friendship> GetPendingAddressedToAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken) {
            var friendship = await _friendshipRepository.GetByIdAsync(friendshipId, cancellationToken);

            // Inexistente ou não endereçado ao usuário → NotFound genérico (não revela pedidos alheios).
            if (friendship is null || friendship.AddresseeId != userId) {
                throw new NotFoundException("Pedido de amizade não encontrado.");
            }

            if (friendship.Status != FriendshipStatus.Pendente) {
                throw new InvalidFriendshipException("Esse pedido já foi respondido.");
            }

            return friendship;
        }

        private static FriendRelationState StateFor(Friendship? existing, Guid userId) {
            if (existing is null) {
                return FriendRelationState.None;
            }

            return existing.Status switch {
                FriendshipStatus.Aceito   => FriendRelationState.Friends,
                FriendshipStatus.Pendente => existing.RequesterId == userId
                    ? FriendRelationState.RequestSent
                    : FriendRelationState.RequestReceived,
                // Recusado se comporta como "sem vínculo": pode pedir de novo.
                _ => FriendRelationState.None
            };
        }

        private async Task<Dictionary<Guid, User>> LoadUsersAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken) {
            var distinct = ids.Distinct().ToList();
            var users = await _userRepository.GetByIdsAsync(distinct, cancellationToken);
            return users.ToDictionary(u => u.Id);
        }

        private static UserSummary ToSummary(User user) => new(user.Id, user.Name, user.Username);
    }
}
