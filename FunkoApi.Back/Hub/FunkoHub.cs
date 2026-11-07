using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FunkoApi.Back.Hub;

/// <summary>
/// Canal de SignalR al que pueden conectarse los administradores de la API.
/// Organiza sus conexiones en grupos y permite el intercambio de mensajes.
/// </summary>
[Authorize(Roles = "ADMIN")]
public class FunkoHub : Microsoft.AspNetCore.SignalR.Hub {
    
    /// <summary>
    /// Incorpora la conexión al grupo del usuario y, si tiene el rol ADMIN, al grupo admins.
    /// </summary>
    /// <returns>Una tarea que finaliza al asignar los grupos y completar la conexión.</returns>
    public override async Task OnConnectedAsync() {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = Context.User?.IsInRole("ADMIN") ?? false;

        if (userId is not null) await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        if (isAdmin) await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        
        await base.OnConnectedAsync();
    }
    
    /// <summary>
    /// Delega la desconexión en SignalR, que retira automáticamente la conexión de sus grupos.
    /// </summary>
    /// <param name="exception">Error que originó el cierre, o null si no hubo error.</param>
    /// <returns>Una tarea que representa la finalización de la desconexión.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception) {
        await base.OnDisconnectedAsync(exception);
    }
    
    /// <summary>
    /// Emite ReceiveMessage a todos los clientes conectados, junto con la fecha UTC.
    /// </summary>
    /// <param name="usuario">Nombre recibido que se mostrará como emisor del mensaje.</param>
    /// <param name="mensaje">Texto que se enviará a las conexiones.</param>
    /// <returns>Una tarea que representa el envío del mensaje.</returns>
    public async Task SendMessage(string usuario, string mensaje) {
        await Clients.All.SendAsync("ReceiveMessage", usuario, mensaje, DateTime.UtcNow);
    }
    
    /// <summary>
    /// Responde únicamente a la conexión que invocó el método, con Sistema como emisor.
    /// </summary>
    /// <param name="mensaje">Texto de la respuesta.</param>
    /// <returns>Una tarea que representa el envío a la conexión de origen.</returns>
    public async Task SendToCaller(string mensaje) {
        await Clients.Caller.SendAsync("ReceiveMessage", "Sistema", mensaje, DateTime.UtcNow);
    }
    
    /// <summary>
    /// Envía ReceiveMessage a las conexiones que pertenecen al grupo indicado.
    /// </summary>
    /// <param name="groupName">Nombre del grupo destinatario.</param>
    /// <param name="mensaje">Texto que se enviará con Sistema como emisor.</param>
    /// <returns>Una tarea que representa el envío al grupo.</returns>
    public async Task SendToGroup(string groupName, string mensaje) {
        await Clients.Group(groupName).SendAsync("ReceiveMessage", "Sistema", mensaje, DateTime.UtcNow);
    }
}