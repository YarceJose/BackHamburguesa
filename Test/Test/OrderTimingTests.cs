using Domain.Enums;
using Domain.Errors;
using Domain.Exceptions;
using Domain.Services;
using static Test.TestData;

namespace Test;

public class OrderTimingTests
{
    [Theory]
    [InlineData(0, 25)]  // empty kitchen
    [InlineData(4, 25)]  // 5th order: still base time
    [InlineData(5, 30)]  // 6th order: +5
    [InlineData(9, 50)]  // 10th order: +25
    public void Estimated_time_grows_with_the_kitchen_load(int ordersInKitchen, int expected) =>
        Assert.Equal(expected, OrderTiming.EstimateMinutes(Settings(), ordersInKitchen));

    [Fact]
    public void Store_closed_by_admin_rejects_orders() =>
        Assert.Equal(OrderTiming.Availability.ClosedByAdmin, OrderTiming.CheckAvailability(Settings(isOpen: false), 0, Now));

    [Fact]
    public void Store_at_capacity_rejects_orders() =>
        Assert.Equal(OrderTiming.Availability.AtCapacity, OrderTiming.CheckAvailability(Settings(maxActive: 3), 3, Now));

    [Fact]
    public void Zero_capacity_means_no_limit() =>
        Assert.Equal(OrderTiming.Availability.Open, OrderTiming.CheckAvailability(Settings(maxActive: 0), 500, Now));

    [Theory]
    [InlineData(20, 59, false)] // 15:59 Colombia: not open yet
    [InlineData(21, 0, true)]   // 16:00 Colombia
    [InlineData(3, 59, true)]   // 22:59 Colombia (UTC is already the next day)
    [InlineData(4, 0, false)]   // 23:00 Colombia: closed
    public void Schedule_uses_Colombia_time(int utcHour, int utcMinute, bool open)
    {
        var day = utcHour < 5 ? Now.Date.AddDays(1) : Now.Date;
        var utc = DateTime.SpecifyKind(day.AddHours(utcHour).AddMinutes(utcMinute), DateTimeKind.Utc);
        Assert.Equal(open, OrderTiming.IsWithinSchedule(Settings(), utc));
    }

    [Fact]
    public void Overnight_schedule_belongs_to_the_opening_day()
    {
        // Open Saturdays 18:00 → 02:00. Sunday 01:00 (Colombia) is still Saturday's shift.
        var settings = Settings(opensAt: new TimeOnly(18, 0), closesAt: new TimeOnly(2, 0), days: [DayOfWeek.Saturday]);
        var sundayOneAm = new DateTime(2026, 11, 8, 6, 0, 0, DateTimeKind.Utc);   // Sun 01:00 COT
        var sundayEvening = new DateTime(2026, 11, 8, 23, 30, 0, DateTimeKind.Utc); // Sun 18:30 COT

        Assert.True(OrderTiming.IsWithinSchedule(settings, sundayOneAm));
        Assert.False(OrderTiming.IsWithinSchedule(settings, sundayEvening));
    }

    [Fact]
    public void Same_opening_and_closing_time_is_invalid() =>
        Assert.Equal(ErrorCodes.StoreSettingsInvalid,
            Assert.Throws<DomainException>(() => Settings(opensAt: new TimeOnly(10, 0), closesAt: new TimeOnly(10, 0))).Code);

    [Fact]
    public void Delay_alert_fires_once_after_the_configured_minutes_then_escalates()
    {
        var settings = Settings(alertAfter: 30, escalateAfter: 5);
        var order = CashOrder(); // received at Now

        Assert.False(OrderTiming.IsDelayAlertDue(order, settings, Now.AddMinutes(29)));
        Assert.True(OrderTiming.IsDelayAlertDue(order, settings, Now.AddMinutes(30)));

        order.MarkDelayAlertSent(Now.AddMinutes(30));
        Assert.False(OrderTiming.IsDelayAlertDue(order, settings, Now.AddMinutes(31)));   // only once
        Assert.False(OrderTiming.IsEscalationDue(order, settings, Now.AddMinutes(34)));
        Assert.True(OrderTiming.IsEscalationDue(order, settings, Now.AddMinutes(35)));    // +5 → admin
    }

    [Fact]
    public void No_alerts_once_delivery_has_the_order()
    {
        var order = CashOrder();
        order.MarkLeftKitchen(7, Now.AddMinutes(10));
        order.MarkPickedUp(8, Now.AddMinutes(12));
        Assert.False(OrderTiming.IsDelayAlertDue(order, Settings(), Now.AddHours(2)));
    }

    [Fact]
    public void Alert_clock_starts_when_the_payment_is_approved()
    {
        var order = TransferOrder();
        order.AttachReceipt("r.jpg", Now);
        order.ApprovePayment(1, Now.AddMinutes(20));
        Assert.False(OrderTiming.IsDelayAlertDue(order, Settings(alertAfter: 30), Now.AddMinutes(40)));
        Assert.True(OrderTiming.IsDelayAlertDue(order, Settings(alertAfter: 30), Now.AddMinutes(50)));
    }

    [Fact]
    public void Unpaid_transfer_expires_but_one_under_review_does_not()
    {
        var settings = Settings(paymentTimeout: 45);
        var unpaid = TransferOrder();
        var underReview = TransferOrder();
        underReview.AttachReceipt("r.jpg", Now.AddMinutes(10));

        Assert.False(OrderTiming.IsPaymentExpired(unpaid, settings, Now.AddMinutes(44)));
        Assert.True(OrderTiming.IsPaymentExpired(unpaid, settings, Now.AddMinutes(45)));
        Assert.False(OrderTiming.IsPaymentExpired(underReview, settings, Now.AddHours(3)));
    }

    [Theory]
    [InlineData(UserRole.Cook, true)]
    [InlineData(UserRole.Delivery, true)]
    [InlineData(UserRole.Admin, false)]
    public void Closing_the_day_revokes_staff_sessions_but_not_the_admin(UserRole role, bool revoked) =>
        Assert.Equal(revoked, OrderTiming.IsSessionRevoked(role, issuedAtUtc: Now, staffSessionsRevokedAt: Now.AddMinutes(1)));

    [Fact]
    public void Tokens_issued_after_closing_the_day_are_valid() =>
        Assert.False(OrderTiming.IsSessionRevoked(UserRole.Cook, Now.AddMinutes(2), Now.AddMinutes(1)));
}
