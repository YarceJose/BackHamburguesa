using Domain.Enums;
using Domain.Errors;
using Domain.Exceptions;

namespace Domain.Entities;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    private Order() { }

    public int Id { get; private set; }

    /// <summary>Short public code: the customer follows the order with it and writes it in the transfer description.</summary>
    public string Code { get; private set; } = null!;

    public int CustomerId { get; private set; }
    public string CustomerName { get; private set; } = null!;
    public string CustomerPhone { get; private set; } = null!;

    /// <summary>Known sector, or null when the customer typed "Otro".</summary>
    public int? SectorId { get; private set; }
    public string SectorName { get; private set; } = null!;

    public OrderStatus Status { get; private set; }
    public decimal Total { get; private set; }
    public int EstimatedMinutes { get; private set; }

    // ---------- Payment ----------
    public PaymentMethod PaymentMethod { get; private set; }
    /// <summary>Cash: the bill the customer will pay with, so delivery brings change.</summary>
    public decimal? CashAmount { get; private set; }
    public string? ReceiptPath { get; private set; }
    public DateTime? ReceiptUploadedAt { get; private set; }
    public DateTime? PaymentApprovedAt { get; private set; }
    public int? PaymentReviewedBy { get; private set; }
    public string? PaymentRejectReason { get; private set; }

    // ---------- Timeline ----------
    public DateTime CreatedAt { get; private set; }
    /// <summary>When the order entered the kitchen queue (cash: at creation; transfer: when payment was approved).</summary>
    public DateTime? ReceivedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? LeftKitchenAt { get; private set; }
    public DateTime? PickedUpAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancelReason { get; private set; }

    public int? CookId { get; private set; }
    public int? DeliveryUserId { get; private set; }

    public DateTime? DelayAlertSentAt { get; private set; }
    public DateTime? EscalationSentAt { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items;

    public static Order Create(string code, User customer, int? sectorId, string sectorName,
        IEnumerable<(Product Product, int Quantity)> lines, PaymentMethod paymentMethod, decimal? cashAmount,
        int estimatedMinutes, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(sectorName))
            throw new DomainException(ErrorCodes.SectorRequired, "The delivery sector is required.");

        var order = new Order
        {
            Code = code,
            CustomerId = customer.Id,
            CustomerName = customer.FullName,
            CustomerPhone = customer.Phone ?? string.Empty,
            SectorId = sectorId,
            SectorName = sectorName.Trim(),
            PaymentMethod = paymentMethod,
            EstimatedMinutes = estimatedMinutes,
            CreatedAt = now
        };

        foreach (var (product, quantity) in lines)
        {
            if (quantity is < 1 or > 50)
                throw new DomainException(ErrorCodes.OrderInvalidQuantity, "Each quantity must be between 1 and 50.");
            order._items.Add(OrderItem.Create(product.Id, product.Name, product.Price, quantity));
        }

        if (order._items.Count == 0)
            throw new DomainException(ErrorCodes.OrderEmpty, "The order has no products.");

        order.Total = order._items.Sum(i => i.Subtotal);

        if (paymentMethod == PaymentMethod.Cash)
        {
            if (cashAmount is not null && cashAmount < order.Total)
                throw new DomainException(ErrorCodes.OrderCashTooLow, "The cash amount is lower than the total.");
            order.CashAmount = cashAmount;
            order.Status = OrderStatus.Received; // cash goes straight to the kitchen
            order.ReceivedAt = now;
        }
        else
        {
            order.Status = OrderStatus.AwaitingPayment;
        }

        return order;
    }

    // ---------- Transfer payment ----------
    /// <summary>The customer uploads (or replaces) the transfer receipt.</summary>
    public void AttachReceipt(string receiptPath, DateTime now)
    {
        if (PaymentMethod != PaymentMethod.Transfer)
            throw new DomainException(ErrorCodes.OrderNotTransfer, "This order is paid in cash.");
        Ensure(OrderStatus.AwaitingPayment, OrderStatus.PaymentReview);
        ReceiptPath = receiptPath;
        ReceiptUploadedAt = now;
        PaymentRejectReason = null;
        Status = OrderStatus.PaymentReview;
    }

    /// <summary>Admin checked the receipt against the bank: the order goes to the kitchen.</summary>
    public void ApprovePayment(int adminId, DateTime now)
    {
        Ensure(OrderStatus.PaymentReview);
        Status = OrderStatus.Received;
        PaymentApprovedAt = now;
        PaymentReviewedBy = adminId;
        ReceivedAt = now;
    }

    /// <summary>Receipt not valid: the customer can upload another one.</summary>
    public void RejectPayment(int adminId, string reason, DateTime now)
    {
        Ensure(OrderStatus.PaymentReview);
        Status = OrderStatus.AwaitingPayment;
        PaymentReviewedBy = adminId;
        PaymentRejectReason = string.IsNullOrWhiteSpace(reason) ? "Comprobante no válido." : reason.Trim();
    }

    // ---------- Kitchen ----------
    public void StartPreparing(int cookId, DateTime now)
    {
        Ensure(OrderStatus.Received);
        Status = OrderStatus.Preparing;
        CookId = cookId;
        StartedAt = now;
    }

    public void MarkLeftKitchen(int cookId, DateTime now)
    {
        Ensure(OrderStatus.Received, OrderStatus.Preparing);
        Status = OrderStatus.LeftKitchen;
        CookId ??= cookId;
        StartedAt ??= now;
        LeftKitchenAt = now;
    }

    // ---------- Delivery ----------
    public void MarkPickedUp(int deliveryUserId, DateTime now)
    {
        Ensure(OrderStatus.LeftKitchen);
        Status = OrderStatus.PickedUp;
        DeliveryUserId = deliveryUserId;
        PickedUpAt = now;
    }

    public void MarkDelivered(DateTime now)
    {
        Ensure(OrderStatus.PickedUp);
        Status = OrderStatus.Delivered;
        DeliveredAt = now;
    }

    // ---------- Admin / system ----------
    public void Cancel(string? reason, DateTime now)
    {
        if (Status.IsFinished())
            throw new DomainException(ErrorCodes.OrderInvalidTransition, $"An order in status {Status} cannot be cancelled.");
        Status = OrderStatus.Cancelled;
        CancelReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        CancelledAt = now;
    }

    // ---------- Alerts (single shot each) ----------
    public void MarkDelayAlertSent(DateTime now) => DelayAlertSentAt ??= now;
    public void MarkEscalationSent(DateTime now) => EscalationSentAt ??= now;

    private void Ensure(params OrderStatus[] allowed)
    {
        if (!allowed.Contains(Status))
            throw new DomainException(ErrorCodes.OrderInvalidTransition,
                $"The order is {Status}; expected {string.Join(" or ", allowed)}.");
    }
}
