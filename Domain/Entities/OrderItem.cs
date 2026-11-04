namespace Domain.Entities;

/// <summary>Order line. Name and price are copied from the product so later menu changes do not alter old orders.</summary>
public sealed class OrderItem
{
    private OrderItem() { }

    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public int ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal Subtotal => UnitPrice * Quantity;

    internal static OrderItem Create(int productId, string productName, decimal unitPrice, int quantity) =>
        new() { ProductId = productId, ProductName = productName, UnitPrice = unitPrice, Quantity = quantity };
}
