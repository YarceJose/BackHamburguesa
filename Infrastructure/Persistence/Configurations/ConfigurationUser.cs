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
        builder.Property(u => u.Username).HasMaxLength(30);
        builder.Property(u => u.SecurityStamp).HasMaxLength(32).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(150);
        builder.Property(u => u.Phone).HasMaxLength(20);
        builder.Property(u => u.PasswordHash).HasMaxLength(100);
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(u => u.IsActive).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();

        builder.Ignore(u => u.CanLogin);

        // Staff sign in with username (stored lowercase); customers have none, so NULLs are allowed
        builder.HasIndex(u => u.Username).IsUnique();

        // Email is stored normalized (lowercase); Postgres allows several NULLs in a unique index.
        builder.HasIndex(u => u.Email).IsUnique();

        // A customer is identified by phone (that is how n8n looks them up).
        builder.HasIndex(u => u.Phone)
            .IsUnique()
            .HasFilter($"\"Role\" = '{nameof(UserRole.Customer)}'")
            .HasDatabaseName("IX_Users_Phone_Customer");

        builder.HasIndex(u => u.Role);
    }
}
