using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDBContext _context;

    public ProductRepository(AppDBContext context) => _context = context;

    public Task<Product?> GetByIdAsync(int id, CancellationToken ct) =>
        _context.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Product?> GetByNameAsync(string name, CancellationToken ct) =>
        _context.Products.FirstOrDefaultAsync(p => p.Name == name, ct);


    public async Task AddAsync(Product product, CancellationToken ct) =>
        await _context.Products.AddAsync(product, ct);

    public void Update(Product product) => _context.Products.Update(product);

    public async Task<List<Product>> GetAllAsync(CancellationToken ct) =>
        await _context.Products.ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) =>
        _context.SaveChangesAsync(ct);
}
