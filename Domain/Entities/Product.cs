using Domain.Errors;
using Domain.Exceptions;

namespace Domain.Entities;

/// <summary>A menu item the admin manages. Old orders keep their own copy of name and price.</summary>
public sealed class Product
{
    private Product() { }                       // required by EF Core

    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; private set; }

    // Menu presentation
    public string? Category { get; private set; }
    public string? Description { get; private set; }
    /// <summary>External picture (a link). Used when there is no uploaded image.</summary>
    public string? ImageUrl { get; private set; }
    /// <summary>Picture uploaded by the admin, relative to the file storage.</summary>
    public string? ImagePath { get; private set; }

    public static Product CreateMenuItem(string name, decimal price, string? category, string? description, string? imageUrl)
    {
        var product = new Product { CreatedAt = DateTime.UtcNow };
        product.UpdateMenuItem(name, price, category, description, imageUrl, isActive: true);
        return product;
    }

    public void UpdateMenuItem(string name, decimal price, string? category, string? description, string? imageUrl, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(ErrorCodes.ProductNameRequired, "Product name is required.");
        if (price <= 0)
            throw new DomainException(ErrorCodes.ProductInvalidPrice, "Price must be greater than zero.");

        Name = name.Trim();
        Price = decimal.Round(price, 2);
        Category = NormalizeCategory(category);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        SetActive(isActive);
    }

    /// <summary>Hidden products disappear from the menu but stay linked to past orders.</summary>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        DeletedAt = isActive ? null : DeletedAt ?? DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetImage(string? imagePath)
    {
        ImagePath = imagePath;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Categories group the menu tabs: "Hamburguesas " and "hamburguesas" are the same one.</summary>
    private static string? NormalizeCategory(string? category) =>
        string.IsNullOrWhiteSpace(category) ? null : category.Trim().ToLowerInvariant();
}
