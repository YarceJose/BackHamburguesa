using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class UserRepository(AppDBContext context) : IUserRepository
{
    public Task<User?> GetByIdAsync(int id, CancellationToken ct) =>
        context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct) =>
        context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

    public Task<User?> GetCustomerByPhoneAsync(string phone, CancellationToken ct) =>
        context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Customer && u.Phone == phone, ct);

    public Task<bool> EmailExistsAsync(string normalizedEmail, int? excludeUserId, CancellationToken ct) =>
        context.Users.AnyAsync(u => u.Email == normalizedEmail && (excludeUserId == null || u.Id != excludeUserId), ct);

    public Task<List<User>> GetAllAsync(UserRole? role, bool includeInactive, CancellationToken ct)
    {
        var query = context.Users.AsNoTracking();
        if (role is not null) query = query.Where(u => u.Role == role);
        if (!includeInactive) query = query.Where(u => u.IsActive);
        return query.OrderBy(u => u.FullName).ToListAsync(ct);
    }

    public Task<int> CountActiveByRoleAsync(UserRole role, CancellationToken ct) =>
        context.Users.CountAsync(u => u.Role == role && u.IsActive, ct);

    public async Task AddAsync(User user, CancellationToken ct) =>
        await context.Users.AddAsync(user, ct);

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
}
