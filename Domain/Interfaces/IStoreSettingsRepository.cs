using Domain.Entities;

namespace Domain.Interfaces;

public interface IStoreSettingsRepository
{
    /// <summary>Returns the single settings row, creating it with defaults if missing.</summary>
    Task<StoreSettings> GetAsync(CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
