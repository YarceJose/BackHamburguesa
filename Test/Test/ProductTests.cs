using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using Domain.Exceptions;

namespace Test;

public class ProductTests
{
    [Fact]
    public void New_product_is_trimmed_and_its_category_normalized()
    {
        var product = Product.CreateMenuItem("  Salchipapa Especial ", 15999.999m, " Salchipapas ", "  ", null);

        Assert.Equal("Salchipapa Especial", product.Name);
        Assert.Equal("salchipapas", product.Category);
        Assert.Equal(16000m, product.Price);
        Assert.Null(product.Description);
        Assert.True(product.IsActive);
    }

    [Theory]
    [InlineData("", 10000, ErrorCodes.ProductNameRequired)]
    [InlineData("Perro", 0, ErrorCodes.ProductInvalidPrice)]
    [InlineData("Perro", -500, ErrorCodes.ProductInvalidPrice)]
    public void Invalid_products_are_rejected(string name, int price, string code) =>
        Assert.Equal(code, Assert.Throws<DomainException>(() => Product.CreateMenuItem(name, price, null, null, null)).Code);

    [Fact]
    public void Hiding_a_product_keeps_when_it_was_hidden_and_showing_it_clears_it()
    {
        var product = TestData.Burger();

        product.SetActive(false);
        var hiddenAt = product.DeletedAt;
        product.SetActive(false);

        Assert.False(product.IsActive);
        Assert.NotNull(hiddenAt);
        Assert.Equal(hiddenAt, product.DeletedAt);

        product.SetActive(true);
        Assert.Null(product.DeletedAt);
    }

    [Fact]
    public void Past_orders_keep_the_price_they_were_sold_at()
    {
        var product = TestData.Burger(price: 19000);
        var order = Order.Create("ABC123", "Ana Cliente", "3009998877", 1, "Centro", [(product, 1)],
            PaymentMethod.Cash, null, 25, TestData.Now);

        product.UpdateMenuItem("Hamburguesa", 25000, "hamburguesas", null, null, isActive: true);

        Assert.Equal(19000m, order.Items.Single().UnitPrice);
    }
}
