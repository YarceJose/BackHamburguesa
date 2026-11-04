using Application.Exceptions;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;

namespace Application.Features.Users;

/// <summary>Reglas compartidas por varios handlers de usuarios.</summary>
public sealed class UserGuard(IUserRepository users)
{
    public async Task<User> FindAsync(int id, CancellationToken ct) =>
        await users.GetByIdAsync(id, ct) ?? throw new NotFoundException($"Usuario {id} no encontrado.");

    public async Task EnsureEmailIsFreeAsync(string email, int? excludeUserId, CancellationToken ct)
    {
        if (await users.EmailExistsAsync(User.NormalizeEmail(email), excludeUserId, ct))
            throw new ConflictException("Ya existe un usuario con ese correo.");
    }

    /// <summary>Evita que el sistema se quede sin administradores.</summary>
    public async Task EnsureNotLastAdminAsync(User admin, CancellationToken ct)
    {
        if (admin.Role == UserRole.Admin && admin.IsActive
            && await users.CountActiveByRoleAsync(UserRole.Admin, ct) <= 1)
            throw new BusinessRuleException("Debe existir al menos un Admin activo.");
    }
}
