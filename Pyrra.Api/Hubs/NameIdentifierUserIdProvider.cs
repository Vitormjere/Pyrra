using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Pyrra.Api.Hubs {
    // Mapeia cada conexão do Hub ao mesmo claim que TryGetUserId já lê em todo controller — é o
    // que faz Clients.User(id) bater com o Id que o resto da API conhece (Fase Admin-4b).
    public class NameIdentifierUserIdProvider : IUserIdProvider {
        public string? GetUserId(HubConnectionContext connection) {
            return connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}
