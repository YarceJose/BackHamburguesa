using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ProductRepository(AppDBContext context) : IProductRepository
{
    public Task<Product?> GetByIdAsync(int id, CancellationToken ct) =>
        context.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    /// <summary>Case-insensitive: "Pechuga" and "pechuga" are the same product.</summary>
    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct) =>
        context.Products.AnyAsync(p => p.Name.ToLower() == name.ToLower() && (excludeId == null || p.Id != excludeId), ct);

    public async Task AddAsync(Product product, CancellationToken ct) =>
        await context.Products.AddAsync(product, ct);

    /// <summary>Admin list: active first, then by category and name.</summary>
    public Task<List<Product>> GetAllAsync(CancellationToken ct) =>
        context.Products.AsNoTracking()
            .OrderByDescending(p => p.IsActive).ThenBy(p => p.Category).ThenBy(p => p.Name)
            .ToListAsync(ct);

    public Task<List<Product>> GetActiveAsync(CancellationToken ct) =>
        context.Products.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Category).ThenBy(p => p.Name).ToListAsync(ct);

    public Task<List<Product>> GetActiveByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        context.Products.AsNoTracking().Where(p => p.IsActive && ids.Contains(p.Id)).ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
}
