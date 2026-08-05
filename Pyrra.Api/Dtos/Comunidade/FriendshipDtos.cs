using System;
using System.ComponentModel.DataAnnotations;
using Pyrra.Application.Comunidade;

namespace Pyrra.Api.Dtos.Comunidade {
    public record UserSummaryResponse(Guid Id, string Name, string? Username) {
        public static UserSummaryResponse FromSummary(UserSummary s) => new(s.Id, s.Name, s.Username);
    }

    public record FriendResponse(Guid FriendshipId, UserSummaryResponse User, DateTime Since) {
        public static FriendResponse FromSummary(FriendSummary s) =>
            new(s.FriendshipId, UserSummaryResponse.FromSummary(s.User), s.Since);
    }

    public record FriendRequestResponse(Guid FriendshipId, UserSummaryResponse Requester, DateTime CreatedAt) {
        public static FriendRequestResponse FromSummary(FriendRequestSummary s) =>
            new(s.FriendshipId, UserSummaryResponse.FromSummary(s.Requester), s.CreatedAt);
    }

    public record UserSearchResultResponse(UserSummaryResponse User, string State) {
        public static UserSearchResultResponse FromResult(UserSearchResult r) =>
            new(UserSummaryResponse.FromSummary(r.User), r.State.ToString());
    }

    public record SendFriendRequestRequest([Required] Guid? AddresseeId);

    public record PendingCountResponse(int Count);

    public record FriendCountResponse(int Count);

    public record InviteLinkResponse(string Token, string Path) {
        public static InviteLinkResponse FromToken(string token) => new(token, $"/convite/{token}");
    }

    public record InviteResultResponse(UserSummaryResponse Owner, string Outcome) {
        public static InviteResultResponse FromResult(InviteResult r) =>
            new(UserSummaryResponse.FromSummary(r.Owner), r.Outcome.ToString());
    }

    // posição no ranking de streak
    public record RankingEntryResponse(int Position, UserSummaryResponse User, int CurrentStreak, bool IsSelf) {
        public static RankingEntryResponse FromEntry(RankingEntry e, int position) =>
            new(position, UserSummaryResponse.FromSummary(e.User), e.CurrentStreak, e.IsSelf);
    }
}