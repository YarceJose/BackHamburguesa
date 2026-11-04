using Application.Abstractions;
using Application.DTOs.Orders;
using Application.Exceptions;
using Application.Features.Store;
using Application.Mediator;
using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using Domain.Interfaces;
using Domain.Services;

namespace Application.Features.Orders;

// ---------- Customer: follow an order by code ----------
public sealed record TrackOrderQuery(string Code) : IRequest<PublicOrderResponse>;

public sealed class TrackOrderHandler(IOrderRepository orders) : IRequestHandler<TrackOrderQuery, PublicOrderResponse>
{
    public async Task<PublicOrderResponse> Handle(TrackOrderQuery q, CancellationToken ct) =>
        PublicOrderResponse.From(await OrderLookup.ByCodeAsync(orders, q.Code, ct));
}

// ---------- Customer: upload the transfer receipt ----------
public sealed record UploadReceiptCommand(string Code, UploadedFile File) : IRequest<PublicOrderResponse>;

public sealed class UploadReceiptHandler(IOrderRepository orders, IFileStorage files, IOrderEvents events, TimeProvider clock)
    : IRequestHandler<UploadReceiptCommand, PublicOrderResponse>
{
    public async Task<PublicOrderResponse> Handle(UploadReceiptCommand cmd, CancellationToken ct)
    {
        var order = await OrderLookup.ByCodeAsync(orders, cmd.Code, ct);
        var (content, extension) = await UploadRules.ReadValidatedAsync(cmd.File, allowPdf: true, ct);

        await using (content)
        {
            var previous = order.ReceiptPath;
            var path = await files.SaveAsync(content, "receipts", extension, ct);
            try
            {
                order.AttachReceipt(path, clock.GetUtcNow().UtcDateTime);
            }
            catch
            {
                await files.DeleteAsync(path, ct); // invalid state: do not keep an orphan file
                throw;
            }
            await orders.SaveChangesAsync(ct);
            if (previous is not null) await files.DeleteAsync(previous, ct);
        }

        await events.PaymentSubmittedAsync(OrderResponse.From(order), ct);
        return PublicOrderResponse.From(order);
    }
}

// ---------- Admin: see, approve or reject the receipt ----------
public sealed record GetReceiptQuery(int OrderId) : IRequest<StoredFile>;

public sealed class GetReceiptHandler(IOrderRepository orders, IFileStorage files) : IRequestHandler<GetReceiptQuery, StoredFile>
{
    public async Task<StoredFile> Handle(GetReceiptQuery q, CancellationToken ct)
    {
        var order = await OrderLookup.ByIdAsync(orders, q.OrderId, ct);
        return (order.ReceiptPath is null ? null : await files.OpenAsync(order.ReceiptPath, ct))
            ?? throw new NotFoundException(ErrorCodes.OrderNotFound, "This order has no receipt.");
    }
}

public sealed record ApprovePaymentCommand(int OrderId, int AdminId) : IRequest<OrderResponse>;

public sealed class ApprovePaymentHandler(IOrderRepository orders,
    StoreStatusReader statusReader, IOrderEvents events, TimeProvider clock) : IRequestHandler<ApprovePaymentCommand, OrderResponse>
{
    public async Task<OrderResponse> Handle(ApprovePaymentCommand cmd, CancellationToken ct)
    {
        var order = await OrderLookup.ByIdAsync(orders, cmd.OrderId, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        order.ApprovePayment(cmd.AdminId, now);
        await orders.SaveChangesAsync(ct);

        var response = OrderResponse.From(order);
        await events.OrderConfirmedAsync(response, ct); // now the kitchen hears about it
        await events.StoreStatusChangedAsync(await statusReader.ReadAsync(ct), ct);
        return response;
    }
}

public sealed record RejectPaymentCommand(int OrderId, int AdminId, string Reason) : IRequest<OrderResponse>;

public sealed class RejectPaymentHandler(IOrderRepository orders, IOrderEvents events, TimeProvider clock)
    : IRequestHandler<RejectPaymentCommand, OrderResponse>
{
    public async Task<OrderResponse> Handle(RejectPaymentCommand cmd, CancellationToken ct)
    {
        var order = await OrderLookup.ByIdAsync(orders, cmd.OrderId, ct);
        order.RejectPayment(cmd.AdminId, cmd.Reason, clock.GetUtcNow().UtcDateTime);
        await orders.SaveChangesAsync(ct);

        var response = OrderResponse.From(order);
        await events.OrderUpdatedAsync(response, ct);
        return response;
    }
}

// ---------- Staff board: open orders + latest finished ----------
public sealed record GetOrderBoardQuery : IRequest<OrderBoardResponse>;

public sealed class GetOrderBoardHandler(IOrderRepository orders) : IRequestHandler<GetOrderBoardQuery, OrderBoardResponse>
{
    public async Task<OrderBoardResponse> Handle(GetOrderBoardQuery q, CancellationToken ct) => new(
        (await orders.GetOpenAsync(ct)).Select(OrderResponse.From).ToList(),
        (await orders.GetRecentFinishedAsync(30, ct)).Select(OrderResponse.From).ToList());
}

// ---------- Kitchen and delivery progress ----------
/// <summary>Start / LeftKitchen: cook or admin. PickedUp / Delivered: delivery or admin.</summary>
public sealed record AdvanceOrderCommand(int OrderId, OrderAction Action, int UserId, UserRole Role) : IRequest<OrderResponse>;

public sealed class AdvanceOrderHandler(IOrderRepository orders, StoreStatusReader statusReader, IOrderEvents events, TimeProvider clock)
    : IRequestHandler<AdvanceOrderCommand, OrderResponse>
{
    public async Task<OrderResponse> Handle(AdvanceOrderCommand cmd, CancellationToken ct)
    {
        var order = await OrderLookup.ByIdAsync(orders, cmd.OrderId, ct);
        var now = clock.GetUtcNow().UtcDateTime;

        var kitchenAction = cmd.Action is OrderAction.Start or OrderAction.LeftKitchen;
        var allowed = cmd.Role == UserRole.Admin || (kitchenAction ? cmd.Role == UserRole.Cook : cmd.Role == UserRole.Delivery);
        if (!allowed)
            throw new ForbiddenException(kitchenAction ? "Only the kitchen can do this." : "Only delivery can do this.");

        switch (cmd.Action)
        {
            case OrderAction.Start: order.StartPreparing(cmd.UserId, now); break;
            case OrderAction.LeftKitchen: order.MarkLeftKitchen(cmd.UserId, now); break;
            case OrderAction.PickedUp: order.MarkPickedUp(cmd.UserId, now); break;
            case OrderAction.Delivered: order.MarkDelivered(now); break;
        }
        await orders.SaveChangesAsync(ct);

        var response = OrderResponse.From(order);
        await events.OrderUpdatedAsync(response, ct);
        await events.StoreStatusChangedAsync(await statusReader.ReadAsync(ct), ct);
        return response;
    }
}

// ---------- Admin: cancel ----------
public sealed record CancelOrderCommand(int OrderId, string? Reason) : IRequest<OrderResponse>;

public sealed class CancelOrderHandler(IOrderRepository orders, StoreStatusReader statusReader, IOrderEvents events, TimeProvider clock)
    : IRequestHandler<CancelOrderCommand, OrderResponse>
{
    public async Task<OrderResponse> Handle(CancelOrderCommand cmd, CancellationToken ct)
    {
        var order = await OrderLookup.ByIdAsync(orders, cmd.OrderId, ct);
        order.Cancel(cmd.Reason, clock.GetUtcNow().UtcDateTime);
        await orders.SaveChangesAsync(ct);

        var response = OrderResponse.From(order);
        await events.OrderUpdatedAsync(response, ct);
        await events.StoreStatusChangedAsync(await statusReader.ReadAsync(ct), ct);
        return response;
    }
}

// ---------- Background: alerts, escalation and unpaid expiry ----------
/// <summary>Run every few seconds. Each alert is sent once per order. Returns how many orders changed.</summary>
public sealed record RunOrderMaintenanceCommand : IRequest<int>;

public sealed class RunOrderMaintenanceHandler(
    IOrderRepository orders, IStoreSettingsRepository settingsRepo, IOrderEvents events, TimeProvider clock)
    : IRequestHandler<RunOrderMaintenanceCommand, int>
{
    private enum Kind { Delayed, Escalated, Expired }

    public async Task<int> Handle(RunOrderMaintenanceCommand cmd, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var settings = await settingsRepo.GetAsync(ct);
        var changed = new List<(Order Order, Kind Kind)>();

        foreach (var order in await orders.GetOpenAsync(ct))
        {
            if (OrderTiming.IsPaymentExpired(order, settings, now))
            {
                order.Cancel("Pago no recibido a tiempo.", now);
                changed.Add((order, Kind.Expired));
            }
            else if (OrderTiming.IsDelayAlertDue(order, settings, now))
            {
                order.MarkDelayAlertSent(now);
                changed.Add((order, Kind.Delayed));
            }
            else if (OrderTiming.IsEscalationDue(order, settings, now))
            {
                order.MarkEscalationSent(now);
                changed.Add((order, Kind.Escalated));
            }
        }

        if (changed.Count == 0) return 0;
        await orders.SaveChangesAsync(ct); // persist first, so a crash never repeats an alert

        foreach (var (order, kind) in changed)
        {
            var response = OrderResponse.From(order);
            var waiting = (int)(now - (order.ReceivedAt ?? order.CreatedAt)).TotalMinutes;
            switch (kind)
            {
                case Kind.Delayed: await events.OrderDelayedAsync(response, waiting, ct); break;
                case Kind.Escalated: await events.OrderEscalatedAsync(response, waiting, ct); break;
                default: await events.OrderUpdatedAsync(response, ct); break;
            }
        }
        return changed.Count;
    }
}

internal static class OrderLookup
{
    public static async Task<Order> ByIdAsync(IOrderRepository orders, int id, CancellationToken ct) =>
        await orders.GetByIdAsync(id, ct) ?? throw new NotFoundException(ErrorCodes.OrderNotFound, $"Order {id} not found.");

    public static async Task<Order> ByCodeAsync(IOrderRepository orders, string code, CancellationToken ct) =>
        await orders.GetByCodeAsync(code.Trim().ToUpperInvariant(), ct)
        ?? throw new NotFoundException(ErrorCodes.OrderNotFound, "Order not found.");
}
