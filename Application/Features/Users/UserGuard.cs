using Application.Exceptions;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Domain.Errors;

namespace Application.Features.Users;

/// <summary>Rules shared by several user handlers.</summary>
public sealed class UserGuard(IUserRepository users)
{
    public async Task<User> FindAsync(int id, CancellationToken ct) =>
        await users.GetByIdAsync(id, ct) ?? throw new NotFoundException(ErrorCodes.UserNotFound, $"User {id} not found.");

    public async Task EnsureEmailIsFreeAsync(string email, int? excludeUserId, CancellationToken ct)
    {
        if (await users.EmailExistsAsync(User.NormalizeEmail(email), excludeUserId, ct))
            throw new ConflictException(ErrorCodes.UserEmailTaken, "A user with that email already exists.");
    }

    public async Task EnsureUsernameIsFreeAsync(string username, int? excludeUserId, CancellationToken ct)
    {
        if (await users.UsernameExistsAsync(User.NormalizeUsername(username), excludeUserId, ct))
            throw new ConflictException(ErrorCodes.UserUsernameTaken, "A user with that username already exists.");
    }

    /// <summary>Prevents the system from being left without administrators.</summary>
    public async Task EnsureNotLastAdminAsync(User admin, CancellationToken ct)
    {
        if (admin.Role == UserRole.Admin && admin.IsActive
            && await users.CountActiveByRoleAsync(UserRole.Admin, ct) <= 1)
            throw new BusinessRuleException(ErrorCodes.UserLastAdmin, "There must be at least one active Admin.");
    }
}
