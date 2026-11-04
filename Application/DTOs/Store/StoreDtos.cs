using System.ComponentModel.DataAnnotations;
using Domain.Entities;

namespace Application.DTOs.Store;

/// <summary>
/// Public: can customers order right now, and how long would it take.
/// Reason: null (open), "closed" (admin closed the day), "outside_schedule" or "at_capacity".
/// </summary>
public sealed record StoreStatusResponse(
    bool IsOpen, bool AcceptingOrders, string? Reason, int EstimatedMinutes, int ActiveOrders,
    TimeOnly OpensAt, TimeOnly ClosesAt, IReadOnlyList<DayOfWeek> OpenDays);

/// <summary>Admin-editable store rules (also the update request). Times are store local time (Colombia).</summary>
public sealed record StoreSettingsDto(
    bool IsOpen,
    TimeOnly OpensAt,
    TimeOnly ClosesAt,
    [Required, MinLength(1)] List<DayOfWeek> OpenDays,
    [Range(1, 240)] int BasePrepMinutes,
    [Range(1, 500)] int OrdersBeforeDelay,
    [Range(0, 120)] int ExtraMinutesPerOrder,
    [Range(0, 1000)] int MaxActiveOrders,
    [Range(1, 480)] int AlertAfterMinutes,
    [Range(1, 240)] int EscalateAfterMinutes,
    [Range(5, 240)] int PaymentTimeoutMinutes)
{
    public static StoreSettingsDto From(StoreSettings s) => new(
        s.IsOpen, s.OpensAt, s.ClosesAt, s.OpenDaySet.OrderBy(d => d).ToList(),
        s.BasePrepMinutes, s.OrdersBeforeDelay, s.ExtraMinutesPerOrder,
        s.MaxActiveOrders, s.AlertAfterMinutes, s.EscalateAfterMinutes, s.PaymentTimeoutMinutes);
}

public sealed record PaymentAccountResponse(
    int Id, string Provider, string HolderName, string AccountReference, bool HasQr, bool IsActive, int SortOrder)
{
    public static PaymentAccountResponse From(PaymentAccount a) =>
        new(a.Id, a.Provider, a.HolderName, a.AccountReference, a.QrImagePath is not null, a.IsActive, a.SortOrder);
}

public sealed record PaymentAccountRequest(
    [Required, StringLength(40, MinimumLength = 2)] string Provider,
    [Required, StringLength(120, MinimumLength = 2)] string HolderName,
    [Required, StringLength(80, MinimumLength = 3)] string AccountReference,
    [Range(0, 1000)] int SortOrder,
    bool IsActive = true);

public sealed record SectorResponse(int Id, string Name, bool IsActive, int SortOrder)
{
    public static SectorResponse From(Sector s) => new(s.Id, s.Name, s.IsActive, s.SortOrder);
}

public sealed record SectorRequest(
    [Required, StringLength(80, MinimumLength = 2)] string Name,
    [Range(0, 1000)] int SortOrder,
    bool IsActive = true);

public sealed record MenuProductResponse(int Id, string Name, string? Category, string? Description, decimal Price, string? ImageUrl)
{
    public static MenuProductResponse From(Product p) => new(p.Id, p.Name, p.Category, p.Description, p.Price, p.ImageUrl);
}
