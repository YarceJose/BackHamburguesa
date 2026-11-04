using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SectorConfiguration : IEntityTypeConfiguration<Sector>
{
    public void Configure(EntityTypeBuilder<Sector> builder)
    {
        builder.ToTable("Sectors");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(80).IsRequired();
        builder.HasIndex(s => s.Name).IsUnique();
    }
}

public class StoreSettingsConfiguration : IEntityTypeConfiguration<StoreSettings>
{
    public void Configure(EntityTypeBuilder<StoreSettings> builder)
    {
        builder.ToTable("StoreSettings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.OpenDays).HasMaxLength(20).IsRequired();
        builder.Ignore(s => s.OpenDaySet);
    }
}

public class PaymentAccountConfiguration : IEntityTypeConfiguration<PaymentAccount>
{
    public void Configure(EntityTypeBuilder<PaymentAccount> builder)
    {
        builder.ToTable("PaymentAccounts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Provider).HasMaxLength(40).IsRequired();
        builder.Property(a => a.HolderName).HasMaxLength(120).IsRequired();
        builder.Property(a => a.AccountReference).HasMaxLength(80).IsRequired();
        builder.Property(a => a.QrImagePath).HasMaxLength(200);
    }
}
