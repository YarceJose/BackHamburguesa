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

/// <summary>Transfer orders come back as AwaitingPayment: the client shows the QR and asks for the receipt.</summary>
public sealed record OrderCreatedResponse(string Code, OrderStatus Status, PaymentMethod PaymentMethod, decimal Total, int EstimatedMinutes, DateTime CreatedAt);

/// <summary>A customer places an order without signing in. The customer record is created or updated by phone.</summary>
public sealed record CreateOrderCommand(CreateOrderRequest Data) : IRequest<OrderCreatedResponse>;

public sealed class CreateOrderHandler(
    IOrderRepository orders,
    IProductRepository products,
    ISectorRepository sectors,
    IUserRepository users,
    IStoreSettingsRepository settingsRepo,
    IPaymentAccountRepository paymentAccounts,
    StoreStatusReader statusReader,
    IOrderEvents events,
    TimeProvider clock) : IRequestHandler<CreateOrderCommand, OrderCreatedResponse>
{
    public async Task<OrderCreatedResponse> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        var r = cmd.Data;
        var now = clock.GetUtcNow().UtcDateTime;

        // 1. Is the store taking orders?
        var settings = await settingsRepo.GetAsync(ct);
        switch (OrderTiming.CheckAvailability(settings, await orders.CountActiveAsync(ct), now))
        {
            case OrderTiming.Availability.ClosedByAdmin:
            case OrderTiming.Availability.OutsideSchedule:
                throw new BusinessRuleException(ErrorCodes.StoreClosed, "The store is not taking orders right now.");
            case OrderTiming.Availability.AtCapacity:
                throw new BusinessRuleException(ErrorCodes.StoreAtCapacity, "The store has reached its order capacity.");
        }

        if (r.PaymentMethod == PaymentMethod.Transfer && (await paymentAccounts.GetAllAsync(onlyActive: true, ct)).Count == 0)
            throw new BusinessRuleException(ErrorCodes.NoPaymentAccounts, "Transfers are not available right now; pay in cash.");

        // 2. Delivery sector: a listed one, or "Otro" with free text
        int? sectorId = null;
        string sectorName;
        if (r.SectorId is { } id)
        {
            var sector = await sectors.GetByIdAsync(id, ct);
            if (sector is null || !sector.IsActive)
                throw new NotFoundException(ErrorCodes.SectorNotFound, "The selected sector does not exist.");
            sectorId = sector.Id;
            sectorName = sector.Name;
        }
        else if (!string.IsNullOrWhiteSpace(r.OtherSector))
        {
            sectorName = r.OtherSector.Trim();
        }
        else
        {
            throw new BusinessRuleException(ErrorCodes.SectorRequired, "Choose your sector or write it.");
        }

        // 3. Prices always come from the database, never from the client
        var lines = r.Items.GroupBy(i => i.ProductId).Select(g => (ProductId: g.Key, Quantity: g.Sum(i => i.Quantity))).ToList();
        var found = (await products.GetActiveByIdsAsync(lines.Select(l => l.ProductId).ToList(), ct)).ToDictionary(p => p.Id);
        if (found.Count != lines.Count)
            throw new NotFoundException(ErrorCodes.ProductNotFound, "One or more products are no longer available.");

        // 4. Customer by phone (created on first order, name refreshed afterwards)
        var phone = r.CustomerPhone.Trim();
        var customer = await users.GetCustomerByPhoneAsync(phone, ct);
        if (customer is null)
        {
            customer = User.CreateCustomer(r.CustomerName, phone, null);
            await users.AddAsync(customer, ct);
        }
        else
        {
            customer.UpdateProfile(r.CustomerName, null, customer.Email, phone);
            customer.Activate();
        }
        await users.SaveChangesAsync(ct);

        // 5. Estimated time grows with the kitchen load
        var estimate = OrderTiming.EstimateMinutes(settings, await orders.CountInKitchenAsync(ct));

        var code = OrderCode.New();
        while (await orders.CodeExistsAsync(code, ct)) code = OrderCode.New();

        var order = Order.Create(code, customer, sectorId, sectorName,
            lines.Select(l => (found[l.ProductId], l.Quantity)), r.PaymentMethod, r.CashAmount, estimate, now);
        await orders.AddAsync(order, ct);
        await orders.SaveChangesAsync(ct);

        await events.OrderCreatedAsync(OrderResponse.From(order), ct);
        if (order.Status == OrderStatus.Received)
            await events.StoreStatusChangedAsync(await statusReader.ReadAsync(ct), ct);

        return new OrderCreatedResponse(order.Code, order.Status, order.PaymentMethod, order.Total, order.EstimatedMinutes, order.CreatedAt);
    }
}
