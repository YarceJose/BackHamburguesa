using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class OrderRepository(AppDBContext context) : IOrderRepository
{
    private static readonly OrderStatus[] Open =
        [OrderStatus.AwaitingPayment, OrderStatus.PaymentReview, OrderStatus.Received, OrderStatus.Preparing, OrderStatus.LeftKitchen, OrderStatus.PickedUp];
    private static readonly OrderStatus[] Active =
        [OrderStatus.Received, OrderStatus.Preparing, OrderStatus.LeftKitchen, OrderStatus.PickedUp];
    private static readonly OrderStatus[] InKitchen = [OrderStatus.Received, OrderStatus.Preparing];

    private IQueryable<Order> WithItems => context.Orders.Include(o => o.Items);

    public Task<Order?> GetByIdAsync(int id, CancellationToken ct) => WithItems.FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Order?> GetByCodeAsync(string code, CancellationToken ct) => WithItems.FirstOrDefaultAsync(o => o.Code == code, ct);

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct) => context.Orders.AnyAsync(o => o.Code == code, ct);

    public Task<List<Order>> GetOpenAsync(CancellationToken ct) =>
        WithItems.Where(o => Open.Contains(o.Status)).OrderBy(o => o.CreatedAt).ToListAsync(ct);

    public Task<List<Order>> GetRecentFinishedAsync(int take, CancellationToken ct) =>
        WithItems.AsNoTracking().Where(o => !Open.Contains(o.Status)).OrderByDescending(o => o.CreatedAt).Take(take).ToListAsync(ct);

    public Task<int> CountInKitchenAsync(CancellationToken ct) => context.Orders.CountAsync(o => InKitchen.Contains(o.Status), ct);

    public Task<int> CountActiveAsync(CancellationToken ct) => context.Orders.CountAsync(o => Active.Contains(o.Status), ct);

    public async Task AddAsync(Order order, CancellationToken ct) => await context.Orders.AddAsync(order, ct);

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
}
