using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Comunidade;

namespace Pyrra.Application.Chat {
    // Chat entre admin e jogadores (Fase Admin-4a) — sem tempo real ainda (chega na 4b). Nenhum
    // método aqui é admin-only: o mesmo serviço atende os dois lados, simétrico. O que restringe a
    // conversar só admin<->jogador é a checagem de par em SendMessageAsync, não um gate de acesso.
    public interface IChatService {
        // Conversas do usuário — uma por contraparte com quem já trocou mensagem, mais recente
        // primeiro. Não inclui gente com quem ainda não há histórico (ver GetActiveAdminsAsync
        // para o jogador escolher com quem começar).
        Task<IReadOnlyList<ChatConversationSummary>> GetConversationsAsync(Guid userId, CancellationToken cancellationToken = default);

        // Histórico completo com uma contraparte específica — só quem participa da conversa
        // consegue ver, porque a busca já é escopada ao par (userId, counterpartId).
        Task<IReadOnlyList<ChatMessageSummary>> GetConversationAsync(Guid userId, Guid counterpartId, CancellationToken cancellationToken = default);

        // Envia e persiste. Exige que exatamente um dos dois lados seja admin — nunca admin-admin
        // nem jogador-jogador.
        Task<ChatMessageSummary> SendMessageAsync(Guid senderId, Guid recipientId, string content, CancellationToken cancellationToken = default);

        // Marca como lidas todas as mensagens dessa conversa que vieram DA contraparte PARA o
        // usuário — chamado ao abrir a tela.
        Task MarkConversationAsReadAsync(Guid userId, Guid counterpartId, CancellationToken cancellationToken = default);

        // Admins ativos (IsAdmin=true, conta não excluída) — usado pelo jogador para saber com quem
        // pode começar uma conversa nova.
        Task<IReadOnlyList<UserSummary>> GetActiveAdminsAsync(CancellationToken cancellationToken = default);
    }
}
