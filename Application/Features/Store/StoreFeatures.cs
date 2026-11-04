using Application.Abstractions;
using Application.DTOs.Store;
using Application.Mediator;
using Domain.Interfaces;
using Domain.Services;

namespace Application.Features.Store;

/// <summary>Computes the public store status from the settings, the schedule and the current load.</summary>
public sealed class StoreStatusReader(IStoreSettingsRepository settingsRepo, IOrderRepository orders, TimeProvider clock)
{
    public async Task<StoreStatusResponse> ReadAsync(CancellationToken ct)
    {
        var settings = await settingsRepo.GetAsync(ct);
        var active = await orders.CountActiveAsync(ct);
        var inKitchen = await orders.CountInKitchenAsync(ct);

        var reason = OrderTiming.CheckAvailability(settings, active, clock.GetUtcNow().UtcDateTime) switch
        {
            OrderTiming.Availability.ClosedByAdmin => "closed",
            OrderTiming.Availability.OutsideSchedule => "outside_schedule",
            OrderTiming.Availability.AtCapacity => "at_capacity",
            _ => null
        };
        return new StoreStatusResponse(settings.IsOpen, reason is null, reason, OrderTiming.EstimateMinutes(settings, inKitchen),
            active, settings.OpensAt, settings.ClosesAt, settings.OpenDaySet.OrderBy(d => d).ToList());
    }
}

// ---------- Public status ----------
public sealed record GetStoreStatusQuery : IRequest<StoreStatusResponse>;

public sealed class GetStoreStatusHandler(StoreStatusReader reader) : IRequestHandler<GetStoreStatusQuery, StoreStatusResponse>
{
    public Task<StoreStatusResponse> Handle(GetStoreStatusQuery q, CancellationToken ct) => reader.ReadAsync(ct);
}

// ---------- Admin settings ----------
public sealed record GetStoreSettingsQuery : IRequest<StoreSettingsDto>;

public sealed class GetStoreSettingsHandler(IStoreSettingsRepository settingsRepo) : IRequestHandler<GetStoreSettingsQuery, StoreSettingsDto>
{
    public async Task<StoreSettingsDto> Handle(GetStoreSettingsQuery q, CancellationToken ct) =>
        StoreSettingsDto.From(await settingsRepo.GetAsync(ct));
}

public sealed record UpdateStoreSettingsCommand(StoreSettingsDto Data) : IRequest<StoreSettingsDto>;

public sealed class UpdateStoreSettingsHandler(
    IStoreSettingsRepository settingsRepo, StoreStatusReader reader, IOrderEvents events, TimeProvider clock)
    : IRequestHandler<UpdateStoreSettingsCommand, StoreSettingsDto>
{
    public async Task<StoreSettingsDto> Handle(UpdateStoreSettingsCommand cmd, CancellationToken ct)
    {
        var d = cmd.Data;
        var settings = await settingsRepo.GetAsync(ct);
        settings.Update(d.IsOpen, d.OpensAt, d.ClosesAt, d.OpenDays, d.BasePrepMinutes, d.OrdersBeforeDelay,
            d.ExtraMinutesPerOrder, d.MaxActiveOrders, d.AlertAfterMinutes, d.EscalateAfterMinutes,
            d.PaymentTimeoutMinutes, clock.GetUtcNow().UtcDateTime);
        await settingsRepo.SaveChangesAsync(ct);

        await events.StoreStatusChangedAsync(await reader.ReadAsync(ct), ct);
        return StoreSettingsDto.From(settings);
    }
}

// ---------- "Cerrar jornada" / "Abrir jornada" ----------
/// <summary>Stops taking orders and signs out kitchen and delivery (admin sessions stay).</summary>
public sealed record CloseDayCommand : IRequest<StoreStatusResponse>;

public sealed class CloseDayHandler(
    IStoreSettingsRepository settingsRepo, StoreStatusReader reader, ISessionRevocation revocation, IOrderEvents events, TimeProvider clock)
    : IRequestHandler<CloseDayCommand, StoreStatusResponse>
{
    public async Task<StoreStatusResponse> Handle(CloseDayCommand cmd, CancellationToken ct)
    {
        var settings = await settingsRepo.GetAsync(ct);
        settings.CloseDay(clock.GetUtcNow().UtcDateTime);
        await settingsRepo.SaveChangesAsync(ct);
        revocation.Set(settings.StaffSessionsRevokedAt);

        var status = await reader.ReadAsync(ct);
        await events.StaffSessionsRevokedAsync(ct);
        await events.StoreStatusChangedAsync(status, ct);
        return status;
    }
}

public sealed record OpenDayCommand : IRequest<StoreStatusResponse>;

public sealed class OpenDayHandler(IStoreSettingsRepository settingsRepo, StoreStatusReader reader, IOrderEvents events, TimeProvider clock)
    : IRequestHandler<OpenDayCommand, StoreStatusResponse>
{
    public async Task<StoreStatusResponse> Handle(OpenDayCommand cmd, CancellationToken ct)
    {
        var settings = await settingsRepo.GetAsync(ct);
        settings.OpenDay(clock.GetUtcNow().UtcDateTime);
        await settingsRepo.SaveChangesAsync(ct);

        var status = await reader.ReadAsync(ct);
        await events.StoreStatusChangedAsync(status, ct);
        return status;
    }
}
