using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Code).HasMaxLength(12).IsRequired();
        builder.HasIndex(o => o.Code).IsUnique();

        builder.Property(o => o.CustomerName).HasMaxLength(120).IsRequired();
        builder.Property(o => o.CustomerPhone).HasMaxLength(20).IsRequired();
        builder.Property(o => o.SectorName).HasMaxLength(80).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.PaymentMethod).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.Total).HasColumnType("numeric(12,2)");
        builder.Property(o => o.CashAmount).HasColumnType("numeric(12,2)");
        builder.Property(o => o.ReceiptPath).HasMaxLength(200);
        builder.Property(o => o.PaymentRejectReason).HasMaxLength(200);
        builder.Property(o => o.CancelReason).HasMaxLength(200);

        builder.HasOne<User>().WithMany().HasForeignKey(o => o.CookId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<User>().WithMany().HasForeignKey(o => o.DeliveryUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Sector>().WithMany().HasForeignKey(o => o.SectorId).OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.CreatedAt);
        builder.HasIndex(o => o.CustomerPhone); // a customer's previous orders
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ProductName).HasMaxLength(150).IsRequired();
        builder.Property(i => i.UnitPrice).HasColumnType("numeric(12,2)");
        builder.Ignore(i => i.Subtotal);
        builder.HasOne<Product>().WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
