using Application.Abstractions;
using Application.DTOs.Orders;
using Application.DTOs.Store;
using Domain.Enums;
using Infrastructure.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace Api.Realtime;

/// <summary>
/// Sends order events to the staff screens (SignalR, by role) and to n8n (WhatsApp).
/// Client method names: orderCreated, paymentSubmitted, orderConfirmed, orderUpdated,
/// orderDelayed, orderEscalated, storeStatus, sessionsRevoked, sessionRevoked (one user).
/// </summary>
public sealed class OrderEventsPublisher(IHubContext<OrdersHub> hub, N8nWebhook n8n, ILogger<OrderEventsPublisher> logger) : IOrderEvents
{
    private static readonly string Admin = OrdersHub.RoleGroup(nameof(UserRole.Admin));
    private static readonly string Cook = OrdersHub.RoleGroup(nameof(UserRole.Cook));
    private static readonly string Delivery = OrdersHub.RoleGroup(nameof(UserRole.Delivery));
    private static readonly string[] AllStaff = [Admin, Cook, Delivery];

    public async Task OrderCreatedAsync(OrderResponse order, CancellationToken ct)
    {
        // Transfer orders only matter to the admin until the payment is approved
        var groups = order.Status == OrderStatus.Received ? AllStaff : [Admin];
        await PushAsync(groups, "orderCreated", order, ct);
        await n8n.SendAsync("order.created", order, ct);
    }

    public async Task PaymentSubmittedAsync(OrderResponse order, CancellationToken ct)
    {
        await PushAsync([Admin], "paymentSubmitted", order, ct);
        await n8n.SendAsync("order.payment_submitted", order, ct);
    }

    public async Task OrderConfirmedAsync(OrderResponse order, CancellationToken ct)
    {
        await PushAsync(AllStaff, "orderConfirmed", order, ct);
        await n8n.SendAsync("order.confirmed", order, ct);
    }

    public async Task OrderUpdatedAsync(OrderResponse order, CancellationToken ct)
    {
        await PushAsync(AllStaff, "orderUpdated", order, ct);
        await n8n.SendAsync("order.updated", order, ct);
    }

    public async Task OrderDelayedAsync(OrderResponse order, int minutesWaiting, CancellationToken ct)
    {
        var payload = new { order, minutesWaiting };
        await PushAsync(AllStaff, "orderDelayed", payload, ct);
        await n8n.SendAsync("order.delayed", payload, ct);
    }

    public async Task OrderEscalatedAsync(OrderResponse order, int minutesWaiting, CancellationToken ct)
    {
        var payload = new { order, minutesWaiting };
        await PushAsync([Admin], "orderEscalated", payload, ct);
        await n8n.SendAsync("order.escalated", payload, ct);
    }

    public Task StoreStatusChangedAsync(StoreStatusResponse status, CancellationToken ct) =>
        PushAsync(AllStaff, "storeStatus", status, ct);

    public Task StaffSessionsRevokedAsync(CancellationToken ct) =>
        PushAsync([Cook, Delivery], "sessionsRevoked", new { at = DateTime.UtcNow }, ct);

    public Task UserSessionRevokedAsync(int userId, CancellationToken ct) =>
        PushAsync([OrdersHub.UserGroup(userId)], "sessionRevoked", new { at = DateTime.UtcNow }, ct);

    private async Task PushAsync(IReadOnlyList<string> groups, string method, object payload, CancellationToken ct)
    {
        try
        {
            await hub.Clients.Groups(groups).SendAsync(method, payload, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not push {Method} to staff screens", method);
        }
    }
}
