using Domain.Errors;
using Domain.Exceptions;

namespace Domain.Entities;

/// <summary>Store-wide operating rules, set by the admin. There is a single row (Id = 1).</summary>
public sealed class StoreSettings
{
    public const int SingletonId = 1;
    /// <summary>The store's local time zone for the opening schedule.</summary>
    public const string TimeZoneId = "America/Bogota";

    private StoreSettings() { }

    public int Id { get; private set; } = SingletonId;

    /// <summary>Manual switch. "Cerrar jornada" turns it off; the admin turns it on again.</summary>
    public bool IsOpen { get; private set; } = true;

    // ---------- Schedule (local time) ----------
    public TimeOnly OpensAt { get; private set; } = new(16, 0);
    public TimeOnly ClosesAt { get; private set; } = new(23, 0);
    /// <summary>Days the store opens, as DayOfWeek numbers (0 = Sunday … 6 = Saturday), e.g. "0,1,2,3,4,5,6".</summary>
    public string OpenDays { get; private set; } = "0,1,2,3,4,5,6";

    // ---------- Load and capacity ----------
    /// <summary>Estimated preparation time with a light kitchen load.</summary>
    public int BasePrepMinutes { get; private set; } = 25;
    /// <summary>From this many orders in the kitchen, every extra order adds <see cref="ExtraMinutesPerOrder"/>.</summary>
    public int OrdersBeforeDelay { get; private set; } = 5;
    public int ExtraMinutesPerOrder { get; private set; } = 5;
    /// <summary>With this many confirmed active orders the store stops taking new ones. 0 = no limit.</summary>
    public int MaxActiveOrders { get; private set; } = 20;

    // ---------- Alerts ----------
    /// <summary>Single soft alert to kitchen and delivery when a confirmed order has not been picked up after these minutes.</summary>
    public int AlertAfterMinutes { get; private set; } = 30;
    /// <summary>Minutes after the first alert to tell the admin the order is still not out.</summary>
    public int EscalateAfterMinutes { get; private set; } = 5;
    /// <summary>Transfer orders without a receipt are cancelled after these minutes.</summary>
    public int PaymentTimeoutMinutes { get; private set; } = 45;

    /// <summary>Staff (cook, delivery) tokens issued before this moment are no longer valid. Admin sessions are kept.</summary>
    public DateTime? StaffSessionsRevokedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public static StoreSettings CreateDefault() => new();

    public IReadOnlySet<DayOfWeek> OpenDaySet =>
        OpenDays.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => int.TryParse(d, out var n) && n is >= 0 and <= 6 ? (DayOfWeek?)n : null)
            .OfType<DayOfWeek>()
            .ToHashSet();

    public void Update(bool isOpen, TimeOnly opensAt, TimeOnly closesAt, IEnumerable<DayOfWeek> openDays,
        int basePrepMinutes, int ordersBeforeDelay, int extraMinutesPerOrder, int maxActiveOrders,
        int alertAfterMinutes, int escalateAfterMinutes, int paymentTimeoutMinutes, DateTime now)
    {
        var days = openDays.Distinct().OrderBy(d => d).ToList();
        Require(opensAt != closesAt, "Opening and closing times must be different.");
        Require(days.Count > 0, "Choose at least one opening day.");
        Require(basePrepMinutes is >= 1 and <= 240, "Base preparation time must be between 1 and 240 minutes.");
        Require(ordersBeforeDelay is >= 1 and <= 500, "Orders before delay must be between 1 and 500.");
        Require(extraMinutesPerOrder is >= 0 and <= 120, "Extra minutes per order must be between 0 and 120.");
        Require(maxActiveOrders is >= 0 and <= 1000, "Max active orders must be between 0 and 1000.");
        Require(alertAfterMinutes is >= 1 and <= 480, "Alert time must be between 1 and 480 minutes.");
        Require(escalateAfterMinutes is >= 1 and <= 240, "Escalation time must be between 1 and 240 minutes.");
        Require(paymentTimeoutMinutes is >= 5 and <= 240, "Payment timeout must be between 5 and 240 minutes.");

        IsOpen = isOpen;
        OpensAt = opensAt;
        ClosesAt = closesAt;
        OpenDays = string.Join(',', days.Select(d => (int)d));
        BasePrepMinutes = basePrepMinutes;
        OrdersBeforeDelay = ordersBeforeDelay;
        ExtraMinutesPerOrder = extraMinutesPerOrder;
        MaxActiveOrders = maxActiveOrders;
        AlertAfterMinutes = alertAfterMinutes;
        EscalateAfterMinutes = escalateAfterMinutes;
        PaymentTimeoutMinutes = paymentTimeoutMinutes;
        UpdatedAt = now;
    }

    /// <summary>"Cerrar jornada": stop taking orders and sign out kitchen and delivery.</summary>
    public void CloseDay(DateTime now)
    {
        IsOpen = false;
        StaffSessionsRevokedAt = now;
        UpdatedAt = now;
    }

    public void OpenDay(DateTime now)
    {
        IsOpen = true;
        UpdatedAt = now;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new DomainException(ErrorCodes.StoreSettingsInvalid, message);
    }
}
