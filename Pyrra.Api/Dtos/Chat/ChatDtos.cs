using System;
using System.ComponentModel.DataAnnotations;
using Pyrra.Api.Dtos.Comunidade;
using Pyrra.Application.Chat;

namespace Pyrra.Api.Dtos.Chat {
    public record ChatMessageResponse(
        Guid      Id,
        UserSummaryResponse Sender,
        Guid      RecipientId,
        string    Content,
        DateTime  CreatedAt,
        DateTime? ReadAt) {
        public static ChatMessageResponse FromSummary(ChatMessageSummary s) => new(
            s.Id, UserSummaryResponse.FromSummary(s.Sender), s.RecipientId, s.Content, s.CreatedAt, s.ReadAt);
    }

    // uma linha por contraparte, pra lista de conversas
    public record ChatConversationResponse(
        UserSummaryResponse Counterpart,
        string   LastMessageContent,
        DateTime LastMessageAt,
        bool     LastMessageFromMe,
        int      UnreadCount) {
        public static ChatConversationResponse FromSummary(ChatConversationSummary s) => new(
            UserSummaryResponse.FromSummary(s.Counterpart), s.LastMessageContent, s.LastMessageAt,
            s.LastMessageFromMe, s.UnreadCount);
    }

    public record SendChatMessageRequest([Required] string Content);
}
