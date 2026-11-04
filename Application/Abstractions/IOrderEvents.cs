using Application.DTOs.Orders;
using Application.DTOs.Store;

namespace Application.Abstractions;

/// <summary>
/// Outgoing notifications: real time to staff screens (SignalR) and to n8n (WhatsApp).
/// Implementations must never throw: a failed notification cannot break an order.
/// </summary>
public interface IOrderEvents
{
    /// <summary>Cash orders arrive confirmed (kitchen); transfer orders arrive awaiting payment (admin).</summary>
    Task OrderCreatedAsync(OrderResponse order, CancellationToken ct);
    /// <summary>The customer uploaded a receipt: the admin must check it.</summary>
    Task PaymentSubmittedAsync(OrderResponse order, CancellationToken ct);
    /// <summary>The admin approved the payment: the order is now in the kitchen queue.</summary>
    Task OrderConfirmedAsync(OrderResponse order, CancellationToken ct);
    Task OrderUpdatedAsync(OrderResponse order, CancellationToken ct);
    Task OrderDelayedAsync(OrderResponse order, int minutesWaiting, CancellationToken ct);
    Task OrderEscalatedAsync(OrderResponse order, int minutesWaiting, CancellationToken ct);
    Task StoreStatusChangedAsync(StoreStatusResponse status, CancellationToken ct);
    /// <summary>"Cerrar jornada": kitchen and delivery screens must sign out.</summary>
    Task StaffSessionsRevokedAsync(CancellationToken ct);
    /// <summary>One user was deactivated or got a new password/role: their screens must sign out.</summary>
    Task UserSessionRevokedAsync(int userId, CancellationToken ct);
}
