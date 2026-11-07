using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FunkoApi.Back.Hub;

[Authorize(Roles = "ADMIN")]
public class FunkoHub : Microsoft.AspNetCore.SignalR.Hub {
    
    public override async Task OnConnectedAsync() {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = Context.User?.IsInRole("ADMIN") ?? false;

        if (userId is not null) await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        if (isAdmin) await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        
        await base.OnConnectedAsync();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception) {
        await base.OnDisconnectedAsync(exception);
    }
    
    public async Task SendMessage(string usuario, string mensaje) {
        await Clients.All.SendAsync("ReceiveMessage", usuario, mensaje, DateTime.UtcNow);
    }
    
    public async Task SendToCaller(string mensaje) {
        await Clients.Caller.SendAsync("ReceiveMessage", "Sistema", mensaje, DateTime.UtcNow);
    }
    
    public async Task SendToGroup(string groupName, string mensaje) {
        await Clients.Group(groupName).SendAsync("ReceiveMessage", "Sistema", mensaje, DateTime.UtcNow);
    }
}