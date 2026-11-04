using Domain.Entities;

namespace Domain.Interfaces;

public interface ISectorRepository
{
    Task<Sector?> GetByIdAsync(int id, CancellationToken ct);
    Task<List<Sector>> GetAllAsync(bool onlyActive, CancellationToken ct);
    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct);
    Task AddAsync(Sector sector, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
