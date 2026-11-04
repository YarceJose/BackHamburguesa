namespace Domain.Entities;
public sealed class Product
{
    private Product() { }                       // requerido por EF Core

    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public void CreateProduct(string name, decimal price, int stock)
    {
        Name = name;
        Price = price;
        Stock = stock;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProduct(string name, decimal price, int stock)
    {
        Name = name;
        Price = price;
        Stock = stock;
        UpdatedAt = DateTime.UtcNow;
    }
    public void DeleteProduct()
    {
        IsActive = false;
        DeletedAt = DateTime.UtcNow;
    }
}