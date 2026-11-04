using Domain.Entities;
using Domain.Enums;

namespace Test;

internal static class TestData
{
    public static readonly DateTime Now = new(2026, 11, 4, 22, 0, 0, DateTimeKind.Utc); // 17:00 in Colombia (Wednesday)

    public static User Customer() => User.CreateCustomer("Ana Cliente", "3009998877", null);

    public static Product Burger(decimal price = 19000) => Product.CreateMenuItem("Hamburguesa", price, "hamburguesas", null, null);

    public static Order CashOrder(decimal? cashAmount = null, int quantity = 2) =>
        Order.Create("ABC123", Customer(), 1, "Centro", [(Burger(), quantity)], PaymentMethod.Cash, cashAmount, 25, Now);

    public static Order TransferOrder() =>
        Order.Create("XYZ789", Customer(), null, "Vereda El Alto", [(Burger(), 1)], PaymentMethod.Transfer, null, 25, Now);

    /// <summary>Defaults: 16:00–23:00 every day, base 25 min, delay from 5 orders (+5 min each), max 20, alert 30, escalate 5, unpaid 45.</summary>
    public static StoreSettings Settings(
        bool isOpen = true, TimeOnly? opensAt = null, TimeOnly? closesAt = null, DayOfWeek[]? days = null,
        int maxActive = 20, int alertAfter = 30, int escalateAfter = 5, int paymentTimeout = 45)
    {
        var settings = StoreSettings.CreateDefault();
        settings.Update(isOpen, opensAt ?? new TimeOnly(16, 0), closesAt ?? new TimeOnly(23, 0), days ?? Enum.GetValues<DayOfWeek>(),
            25, 5, 5, maxActive, alertAfter, escalateAfter, paymentTimeout, Now);
        return settings;
    }
}
