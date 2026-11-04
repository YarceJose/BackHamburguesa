using System.ComponentModel.DataAnnotations;
using Domain.Entities;
using Domain.Enums;

namespace Application.DTOs.Orders;

public sealed record OrderLineRequest(int ProductId, int Quantity);

/// <summary>
/// Public order (no sign-in). Rural delivery: the customer picks a sector (vereda/barrio) from the list,
/// or sends <see cref="OtherSector"/> when theirs is not listed. Payment: cash on delivery or transfer (QR + receipt).
/// </summary>
public sealed record CreateOrderRequest(
    [Required, StringLength(120, MinimumLength = 2)] string CustomerName,
    [Required, Phone, StringLength(20, MinimumLength = 7)] string CustomerPhone,
    int? SectorId,
    [StringLength(80)] string? OtherSector,
    [Required, MinLength(1), MaxLength(30)] List<OrderLineRequest> Items,
    [Required, EnumDataType(typeof(PaymentMethod))] PaymentMethod PaymentMethod,
    [Range(0, 10_000_000)] decimal? CashAmount);

public sealed record RejectPaymentRequest([Required, StringLength(200, MinimumLength = 3)] string Reason);

public sealed record CancelOrderRequest([StringLength(200)] string? Reason);

public sealed record OrderItemResponse(int ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal Subtotal);

/// <summary>Full order for staff views and real-time notifications.</summary>
public sealed record OrderResponse(
    int Id,
    string Code,
    OrderStatus Status,
    string CustomerName,
    string CustomerPhone,
    string SectorName,
    IReadOnlyList<OrderItemResponse> Items,
    decimal Total,
    int EstimatedMinutes,
    PaymentMethod PaymentMethod,
    decimal? CashAmount,
    bool HasReceipt,
    string? PaymentRejectReason,
    DateTime CreatedAt,
    DateTime? ReceivedAt,
    DateTime? StartedAt,
    DateTime? LeftKitchenAt,
    DateTime? PickedUpAt,
    DateTime? DeliveredAt,
    DateTime? CancelledAt,
    string? CancelReason,
    int? CookId,
    int? DeliveryUserId,
    bool DelayAlertSent,
    bool EscalationSent)
{
    public static OrderResponse From(Order o) => new(
        o.Id, o.Code, o.Status, o.CustomerName, o.CustomerPhone, o.SectorName,
        o.Items.Select(i => new OrderItemResponse(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.Subtotal)).ToList(),
        o.Total, o.EstimatedMinutes, o.PaymentMethod, o.CashAmount, o.ReceiptPath is not null, o.PaymentRejectReason,
        o.CreatedAt, o.ReceivedAt, o.StartedAt, o.LeftKitchenAt, o.PickedUpAt, o.DeliveredAt,
        o.CancelledAt, o.CancelReason, o.CookId, o.DeliveryUserId, o.DelayAlertSentAt is not null, o.EscalationSentAt is not null);
}

/// <summary>What the customer sees when following an order by its code (no phone number).</summary>
public sealed record PublicOrderResponse(
    string Code,
    OrderStatus Status,
    string CustomerFirstName,
    string SectorName,
    IReadOnlyList<OrderItemResponse> Items,
    decimal Total,
    int EstimatedMinutes,
    PaymentMethod PaymentMethod,
    string? PaymentRejectReason,
    DateTime CreatedAt,
    DateTime? ReceivedAt,
    DateTime? LeftKitchenAt,
    DateTime? PickedUpAt,
    DateTime? DeliveredAt,
    DateTime? CancelledAt)
{
    public static PublicOrderResponse From(Order o) => new(
        o.Code, o.Status, o.CustomerName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? o.CustomerName,
        o.SectorName,
        o.Items.Select(i => new OrderItemResponse(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.Subtotal)).ToList(),
        o.Total, o.EstimatedMinutes, o.PaymentMethod, o.PaymentRejectReason,
        o.CreatedAt, o.ReceivedAt, o.LeftKitchenAt, o.PickedUpAt, o.DeliveredAt, o.CancelledAt);
}

public sealed record OrderBoardResponse(IReadOnlyList<OrderResponse> Active, IReadOnlyList<OrderResponse> RecentFinished);

public enum OrderAction
{
    Start,
    LeftKitchen,
    PickedUp,
    Delivered
}
