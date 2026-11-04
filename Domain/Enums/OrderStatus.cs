namespace Domain.Enums;

/// <summary>
/// Order lifecycle.
/// Cash:     Received → Preparing → LeftKitchen (cook) → PickedUp (delivery has it) → Delivered.
/// Transfer: AwaitingPayment → PaymentReview (receipt uploaded) → Received (admin approved) → … same as cash.
/// Cancelled can happen any time before delivery (admin, or expired unpaid orders).
/// </summary>
public enum OrderStatus
{
    AwaitingPayment = 1,
    PaymentReview = 2,
    Received = 3,
    Preparing = 4,
    LeftKitchen = 5,
    PickedUp = 6,
    Delivered = 7,
    Cancelled = 8
}

public enum PaymentMethod
{
    Cash = 1,
    Transfer = 2
}

public static class OrderStatusExtensions
{
    /// <summary>Waiting for the customer's payment or for the admin to check the receipt.</summary>
    public static bool IsPendingPayment(this OrderStatus status) => status is OrderStatus.AwaitingPayment or OrderStatus.PaymentReview;

    /// <summary>In the kitchen queue: these orders make the estimated time grow.</summary>
    public static bool IsInKitchen(this OrderStatus status) => status is OrderStatus.Received or OrderStatus.Preparing;

    /// <summary>Confirmed and not finished: counts toward the store capacity.</summary>
    public static bool IsActive(this OrderStatus status) =>
        status is OrderStatus.Received or OrderStatus.Preparing or OrderStatus.LeftKitchen or OrderStatus.PickedUp;

    /// <summary>Delay alerts apply from confirmation until the delivery person has the order.</summary>
    public static bool IsAwaitingDispatch(this OrderStatus status) =>
        status is OrderStatus.Received or OrderStatus.Preparing or OrderStatus.LeftKitchen;

    public static bool IsFinished(this OrderStatus status) => status is OrderStatus.Delivered or OrderStatus.Cancelled;
}
