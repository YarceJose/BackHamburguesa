using Domain.Entities;

namespace Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken ct);
    Task<Order?> GetByCodeAsync(string code, CancellationToken ct);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct);
    /// <summary>Everything not finished: pending payment, in kitchen, out for delivery.</summary>
    Task<List<Order>> GetOpenAsync(CancellationToken ct);
    Task<List<Order>> GetRecentFinishedAsync(int take, CancellationToken ct);
    Task<int> CountInKitchenAsync(CancellationToken ct);
    Task<int> CountActiveAsync(CancellationToken ct);
    Task AddAsync(Order order, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
