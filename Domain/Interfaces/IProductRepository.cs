using Domain.Entities;

namespace Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken ct);
    Task<Product?> GetByNameAsync(string name, CancellationToken ct);
    Task AddAsync(Product product, CancellationToken ct);
    void Update(Product product);
    Task<List<Product>> GetAllAsync(CancellationToken ct);
}