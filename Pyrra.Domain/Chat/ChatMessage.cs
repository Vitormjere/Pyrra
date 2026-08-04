using System;

namespace Pyrra.Domain.Chat {
    // Mensagem entre um admin e um jogador (Fase Admin-4a). Sem entidade de "conversa" separada:
    // o par (SenderId, RecipientId) já identifica a conversa — o histórico é a soma das mensagens
    // nos dois sentidos entre duas pessoas. Sempre um admin de um lado e um jogador do outro (nunca
    // admin-admin nem jogador-jogador), validado no serviço, não aqui.
    public class ChatMessage {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public Guid RecipientId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Nulo = não lida. Marcada em lote ao abrir a conversa (ChatService.MarkConversationAsReadAsync),
        // não mensagem por mensagem.
        public DateTime? ReadAt { get; set; }
    }
}
