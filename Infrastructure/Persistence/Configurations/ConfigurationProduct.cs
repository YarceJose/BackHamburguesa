using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
        builder.Property(p => p.Stock).IsRequired(); // int column (was numeric by mistake)
        builder.Property(p => p.IsActive).IsRequired();
        builder.Property(p => p.Price).HasColumnType("numeric(10,2)").IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
        builder.Property(p => p.DeletedAt).IsRequired(false);
        builder.Property(p => p.Category).HasMaxLength(40);
        builder.Property(p => p.Description).HasMaxLength(300);
        builder.Property(p => p.ImageUrl).HasMaxLength(500);
    }
}