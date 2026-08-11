using System;
using System.ComponentModel.DataAnnotations;
using Pyrra.Api.Dtos.Comunidade;
using Pyrra.Application.Chat;

namespace Pyrra.Api.Dtos.Chat {
    public record TeamChatMessageResponse(
        Guid      Id,
        UserSummaryResponse Sender,
        Guid      TeamId,
        string    Content,
        DateTime  CreatedAt) {
        public static TeamChatMessageResponse FromSummary(TeamChatMessageSummary s) => new(
            s.Id, UserSummaryResponse.FromSummary(s.Sender), s.TeamId, s.Content, s.CreatedAt);
    }

    public record SendTeamChatMessageRequest([Required] string Content);
}
