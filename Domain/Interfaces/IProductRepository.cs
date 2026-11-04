using Domain.Entities;

namespace Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken ct);
    Task<Product?> GetByNameAsync(string name, CancellationToken ct);
    Task AddAsync(Product product, CancellationToken ct);
    void Update(Product product);
    Task<List<Product>> GetAllAsync(CancellationToken ct);
    Task<List<Product>> GetActiveAsync(CancellationToken ct);
    Task<List<Product>> GetActiveByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
}