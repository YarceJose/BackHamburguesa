using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken ct);
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct);
    Task<User?> GetByUsernameAsync(string normalizedUsername, CancellationToken ct);
    Task<bool> UsernameExistsAsync(string normalizedUsername, int? excludeUserId, CancellationToken ct);
    Task<bool> EmailExistsAsync(string normalizedEmail, int? excludeUserId, CancellationToken ct);
    Task<List<User>> GetAllAsync(UserRole? role, bool includeInactive, CancellationToken ct);
    Task<int> CountActiveByRoleAsync(UserRole role, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
