using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName).IsRequired().HasMaxLength(120);
        builder.Property(u => u.Email).HasMaxLength(150);
        builder.Property(u => u.Phone).HasMaxLength(20);
        builder.Property(u => u.PasswordHash).HasMaxLength(100);
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(u => u.IsActive).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();

        builder.Ignore(u => u.CanLogin);

        // El correo se guarda normalizado (minúsculas); Postgres permite varios NULL en un índice único.
        builder.HasIndex(u => u.Email).IsUnique();

        // Un cliente se identifica por su teléfono (n8n lo busca así).
        builder.HasIndex(u => u.Phone)
            .IsUnique()
            .HasFilter($"\"Role\" = '{nameof(UserRole.Customer)}'")
            .HasDatabaseName("IX_Users_Phone_Customer");

        builder.HasIndex(u => u.Role);
    }
}
