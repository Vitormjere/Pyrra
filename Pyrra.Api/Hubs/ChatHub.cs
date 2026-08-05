using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Pyrra.Api.Hubs {
    // canal só de push pro chat em tempo real 
    [Authorize]
    public class ChatHub : Hub {
    }
}
