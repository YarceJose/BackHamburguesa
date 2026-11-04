using Domain.Entities;
using Domain.Interfaces;
namespace Application.UseCases;

public class CaseUseProduct
{
    private readonly IProductRepository _productRepository;

    public CaseUseProduct(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Product?> GetProductByIdAsync(int id, CancellationToken ct)
    {
        return await _productRepository.GetByIdAsync(id, ct);
    }

    public async Task<Product?> GetProductByNameAsync(string name, CancellationToken ct)
    {
        return await _productRepository.GetByNameAsync(name, ct);
    }

    public async Task AddProductAsync(Product product, CancellationToken ct)
    {
        await _productRepository.AddAsync(product, ct);
    }

    public void UpdateProduct(Product product)
    {
        _productRepository.Update(product);
    }

    public async Task<List<Product>> GetAllProductsAsync(CancellationToken ct)
    {
        return await _productRepository.GetAllAsync(ct);
    }
}