using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Pyrra.Api.Hubs {
    // Usa o mesmo ID da API no Hub
    public class NameIdentifierUserIdProvider : IUserIdProvider {
        public string? GetUserId(HubConnectionContext connection) {
            return connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}
