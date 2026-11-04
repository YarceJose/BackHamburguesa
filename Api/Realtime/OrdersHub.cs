using Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Api.Realtime;

/// <summary>
/// Real-time channel for staff screens. Each connection joins the group of its role
/// (Admin, Cook, Delivery) so every view only gets what it needs.
/// </summary>
[Authorize(Policy = Policies.Staff)]
public sealed class OrdersHub : Hub
{
    public const string Path = "/hubs/orders";

    public static string RoleGroup(string role) => $"role:{role}";

    /// <summary>All open screens of one user (to sign them out when their access changes).</summary>
    public static string UserGroup(int userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirst("role")?.Value;
        if (!string.IsNullOrEmpty(role))
            await Groups.AddToGroupAsync(Context.ConnectionId, RoleGroup(role));
        if (int.TryParse(Context.User?.FindFirst("sub")?.Value, out var userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        await base.OnConnectedAsync();
    }
}
