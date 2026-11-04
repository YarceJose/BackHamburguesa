using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class SectorRepository(AppDBContext context) : ISectorRepository
{
    public Task<Sector?> GetByIdAsync(int id, CancellationToken ct) => context.Sectors.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<List<Sector>> GetAllAsync(bool onlyActive, CancellationToken ct) =>
        context.Sectors.AsNoTracking().Where(s => !onlyActive || s.IsActive).OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync(ct);

    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct) =>
        context.Sectors.AnyAsync(s => s.Name.ToLower() == name.ToLower() && (excludeId == null || s.Id != excludeId), ct);

    public async Task AddAsync(Sector sector, CancellationToken ct) => await context.Sectors.AddAsync(sector, ct);

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
}

public class StoreSettingsRepository(AppDBContext context) : IStoreSettingsRepository
{
    public async Task<StoreSettings> GetAsync(CancellationToken ct)
    {
        var settings = await context.StoreSettings.FirstOrDefaultAsync(s => s.Id == StoreSettings.SingletonId, ct);
        if (settings is not null) return settings;

        settings = StoreSettings.CreateDefault();
        context.StoreSettings.Add(settings);
        await context.SaveChangesAsync(ct);
        return settings;
    }

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
}

public class PaymentAccountRepository(AppDBContext context) : IPaymentAccountRepository
{
    public Task<PaymentAccount?> GetByIdAsync(int id, CancellationToken ct) =>
        context.PaymentAccounts.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<List<PaymentAccount>> GetAllAsync(bool onlyActive, CancellationToken ct) =>
        context.PaymentAccounts.AsNoTracking().Where(a => !onlyActive || a.IsActive)
            .OrderBy(a => a.SortOrder).ThenBy(a => a.Provider).ToListAsync(ct);

    public async Task AddAsync(PaymentAccount account, CancellationToken ct) => await context.PaymentAccounts.AddAsync(account, ct);

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
}
