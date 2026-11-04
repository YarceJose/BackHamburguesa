using Domain.Entities;

namespace Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken ct);
    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct);
    Task AddAsync(Product product, CancellationToken ct);
    Task<List<Product>> GetAllAsync(CancellationToken ct);
    Task<List<Product>> GetActiveAsync(CancellationToken ct);
    Task<List<Product>> GetActiveByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}