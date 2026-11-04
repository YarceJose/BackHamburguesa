using Domain.Entities;
using Domain.Enums;

namespace Domain.Services;

/// <summary>Pure rules for schedule, capacity, estimated times and alerts (easy to unit test).</summary>
public static class OrderTiming
{
    public enum Availability { Open, ClosedByAdmin, OutsideSchedule, AtCapacity }

    private static readonly TimeZoneInfo StoreZone = ResolveZone();

    /// <summary>
    /// Estimated minutes for a new order given how many orders are already in the kitchen.
    /// Orders up to <c>OrdersBeforeDelay</c> keep the base time; each one beyond adds the extra minutes.
    /// </summary>
    public static int EstimateMinutes(StoreSettings settings, int ordersInKitchen)
    {
        var position = ordersInKitchen + 1; // this new order
        var overflow = Math.Max(0, position - settings.OrdersBeforeDelay);
        return settings.BasePrepMinutes + overflow * settings.ExtraMinutesPerOrder;
    }

    public static Availability CheckAvailability(StoreSettings settings, int activeOrders, DateTime utcNow)
    {
        if (!settings.IsOpen) return Availability.ClosedByAdmin;
        if (!IsWithinSchedule(settings, utcNow)) return Availability.OutsideSchedule;
        if (settings.MaxActiveOrders > 0 && activeOrders >= settings.MaxActiveOrders) return Availability.AtCapacity;
        return Availability.Open;
    }

    /// <summary>Supports schedules that cross midnight (e.g. 18:00 → 02:00 belongs to the opening day).</summary>
    public static bool IsWithinSchedule(StoreSettings settings, DateTime utcNow)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), StoreZone);
        var time = TimeOnly.FromDateTime(local);
        var days = settings.OpenDaySet;

        if (settings.OpensAt < settings.ClosesAt)
            return days.Contains(local.DayOfWeek) && time >= settings.OpensAt && time < settings.ClosesAt;

        // Overnight: evening part today, early-morning part belongs to yesterday's opening
        return (days.Contains(local.DayOfWeek) && time >= settings.OpensAt)
            || (days.Contains(local.AddDays(-1).DayOfWeek) && time < settings.ClosesAt);
    }

    /// <summary>True once, when a confirmed order has waited the configured minutes without being picked up.</summary>
    public static bool IsDelayAlertDue(Order order, StoreSettings settings, DateTime now) =>
        order.Status.IsAwaitingDispatch()
        && order.ReceivedAt is not null
        && order.DelayAlertSentAt is null
        && now >= order.ReceivedAt.Value.AddMinutes(settings.AlertAfterMinutes);

    /// <summary>True once, when the extra minutes after the first alert have passed and it is still not picked up.</summary>
    public static bool IsEscalationDue(Order order, StoreSettings settings, DateTime now) =>
        order.Status.IsAwaitingDispatch()
        && order.DelayAlertSentAt is not null
        && order.EscalationSentAt is null
        && now >= order.DelayAlertSentAt.Value.AddMinutes(settings.EscalateAfterMinutes);

    /// <summary>Transfer orders never paid are cancelled after the timeout (orders under review wait for the admin).</summary>
    public static bool IsPaymentExpired(Order order, StoreSettings settings, DateTime now) =>
        order.Status == OrderStatus.AwaitingPayment && now >= order.CreatedAt.AddMinutes(settings.PaymentTimeoutMinutes);

    /// <summary>Staff tokens issued before the last "Cerrar jornada" are rejected; admin tokens never are.</summary>
    public static bool IsSessionRevoked(UserRole role, DateTime issuedAtUtc, DateTime? staffSessionsRevokedAt) =>
        role != UserRole.Admin && staffSessionsRevokedAt is { } revokedAt && issuedAtUtc < revokedAt;

    private static TimeZoneInfo ResolveZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(StoreSettings.TimeZoneId); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.CreateCustomTimeZone("COT", TimeSpan.FromHours(-5), "Colombia", "Colombia"); }
    }
}
