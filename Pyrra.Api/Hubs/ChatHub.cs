using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Pyrra.Api.Hubs {
    // Canal de notificação em tempo real do chat (Fase Admin-4b) — só push, sem métodos
    // client->server: enviar mensagem continua sendo só o REST existente (ChatController).
    // A autenticação é a mesma JWT de todo o resto da API, só chega via query string no handshake
    // (ver Program.cs, JwtBearerEvents.OnMessageReceived) porque o navegador não permite header
    // custom no handshake de WebSocket.
    [Authorize]
    public class ChatHub : Hub {
    }
}
