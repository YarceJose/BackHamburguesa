using Microsoft.EntityFrameworkCore;
using Domain.Entities;
namespace Infrastructure.Persistence;
public class AppDBContext : DbContext
{
    public AppDBContext(DbContextOptions<AppDBContext> options) : base(options)
    {
    }
    public DbSet<Product> Products => Set<Product>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Sector> Sectors => Set<Sector>();
    public DbSet<StoreSettings> StoreSettings => Set<StoreSettings>();
    public DbSet<PaymentAccount> PaymentAccounts => Set<PaymentAccount>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDBContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}