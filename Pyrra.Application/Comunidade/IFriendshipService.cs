using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Comunidade {
    public interface IFriendshipService {
        // Busca por username ou email (o email casa mas nunca volta na resposta), excluindo o próprio
        // usuário, com o estado do vínculo de cada resultado para a UI escolher o botão.
        Task<IReadOnlyList<UserSearchResult>> SearchUsersAsync(Guid userId, string term, CancellationToken cancellationToken = default);

        // Envia um pedido. Lança InvalidFriendshipException para si mesmo, duplicado, já amigos ou
        // pedido recíproco pendente; NotFoundException se o destinatário não existir.
        Task SendRequestAsync(Guid requesterId, Guid addresseeId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<FriendRequestSummary>> GetPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<int> GetPendingReceivedCountAsync(Guid userId, CancellationToken cancellationToken = default);

        // Aceita/recusa um pedido pendente. Só o destinatário do pedido pode; aceitar ou recusar o
        // que já foi respondido lança InvalidFriendshipException.
        Task AcceptAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default);
        Task DeclineAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<FriendSummary>> GetFriendsAsync(Guid userId, CancellationToken cancellationToken = default);

        // Só a contagem, para o número de amigos do Perfil.
        Task<int> GetFriendsCountAsync(Guid userId, CancellationToken cancellationToken = default);

        // Remove/desfaz — funciona para amizade aceita (desfazer) e para pedido enviado (cancelar).
        // Qualquer um dos dois participantes pode.
        Task RemoveAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default);

        // Link de convite pessoal: devolve o token estável do usuário, criando-o na primeira vez.
        Task<string> GetOrCreateInviteTokenAsync(Guid userId, CancellationToken cancellationToken = default);

        // Abrir um convite: resolve o dono do token e envia o pedido, devolvendo o desfecho (sem
        // tratar duplicado/já-amigos como erro — o link é idempotente). Token inválido → NotFound.
        Task<InviteResult> AcceptInviteAsync(Guid userId, string inviteToken, CancellationToken cancellationToken = default);
    }
}
