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
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDBContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}