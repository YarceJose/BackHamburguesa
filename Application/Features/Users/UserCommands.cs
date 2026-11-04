using Application.DTOs.Users;
using Application.Exceptions;
using Application.Mediator;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Domain.Ports;
using Domain.Errors;

namespace Application.Features.Users;

// ---------- Create ----------
/// <summary>Every user (admin, cook, delivery) needs a username and a password.</summary>
public sealed record CreateUserCommand(CreateUserRequest Data) : IRequest<UserResponse>;

public sealed class CreateUserHandler(IUserRepository users, IPasswordHasher hasher, UserGuard guard)
    : IRequestHandler<CreateUserCommand, UserResponse>
{
    public async Task<UserResponse> Handle(CreateUserCommand cmd, CancellationToken ct)
    {
        var r = cmd.Data;
        if (string.IsNullOrWhiteSpace(r.Username))
            throw new BusinessRuleException(ErrorCodes.UserUsernameRequired, "Username is required.");
        if (string.IsNullOrWhiteSpace(r.Password))
            throw new BusinessRuleException(ErrorCodes.UserPasswordRequired, "Password is required.");

        await guard.EnsureUsernameIsFreeAsync(r.Username, null, ct);
        if (!string.IsNullOrWhiteSpace(r.Email))
            await guard.EnsureEmailIsFreeAsync(r.Email, null, ct);
        var user = User.CreateStaff(r.FullName, r.Username, r.Email, r.Phone, hasher.Hash(r.Password), r.Role);

        await users.AddAsync(user, ct);
        await users.SaveChangesAsync(ct);
        return UserResponse.From(user);
    }
}

// ---------- Update ----------
public sealed record UpdateUserCommand(int Id, int CurrentUserId, UpdateUserRequest Data) : IRequest<UserResponse>;

public sealed class UpdateUserHandler(IUserRepository users, UserGuard guard, UserSessions sessions)
    : IRequestHandler<UpdateUserCommand, UserResponse>
{
    public async Task<UserResponse> Handle(UpdateUserCommand cmd, CancellationToken ct)
    {
        var r = cmd.Data;
        var user = await guard.FindAsync(cmd.Id, ct);

        if (!string.IsNullOrWhiteSpace(r.Email))
            await guard.EnsureEmailIsFreeAsync(r.Email, user.Id, ct);
        if (!string.IsNullOrWhiteSpace(r.Username))
            await guard.EnsureUsernameIsFreeAsync(r.Username, user.Id, ct);

        if (user.Role == UserRole.Admin && r.Role != UserRole.Admin)
        {
            if (user.Id == cmd.CurrentUserId)
                throw new BusinessRuleException(ErrorCodes.UserCannotDemoteSelf, "You cannot remove your own Admin role.");
            await guard.EnsureNotLastAdminAsync(user, ct);
        }

        var stamp = user.SecurityStamp;
        user.UpdateProfile(r.FullName, r.Username ?? user.Username, r.Email, r.Phone);
        user.ChangeRole(r.Role);

        await users.SaveChangesAsync(ct);
        await sessions.RevokeIfChangedAsync(user, stamp, ct); // a new role signs the user out
        return UserResponse.From(user);
    }
}

// ---------- Password reset (set by an Admin) ----------
public sealed record ResetPasswordCommand(int Id, ResetPasswordRequest Data) : IRequest;

public sealed class ResetPasswordHandler(IUserRepository users, IPasswordHasher hasher, UserGuard guard, UserSessions sessions)
    : IRequestHandler<ResetPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ResetPasswordCommand cmd, CancellationToken ct)
    {
        var user = await guard.FindAsync(cmd.Id, ct);
        var stamp = user.SecurityStamp;
        user.ChangePassword(hasher.Hash(cmd.Data.NewPassword));
        await users.SaveChangesAsync(ct);
        await sessions.RevokeIfChangedAsync(user, stamp, ct);
        return Unit.Value;
    }
}

// ---------- Deactivate (soft delete) ----------
public sealed record DeactivateUserCommand(int Id, int CurrentUserId) : IRequest;

public sealed class DeactivateUserHandler(IUserRepository users, UserGuard guard, UserSessions sessions)
    : IRequestHandler<DeactivateUserCommand, Unit>
{
    public async Task<Unit> Handle(DeactivateUserCommand cmd, CancellationToken ct)
    {
        var user = await guard.FindAsync(cmd.Id, ct);

        if (user.Id == cmd.CurrentUserId)
            throw new BusinessRuleException(ErrorCodes.UserCannotDeactivateSelf, "You cannot deactivate your own user.");
        await guard.EnsureNotLastAdminAsync(user, ct);

        var stamp = user.SecurityStamp;
        user.Deactivate();
        await users.SaveChangesAsync(ct);
        await sessions.RevokeIfChangedAsync(user, stamp, ct);
        return Unit.Value;
    }
}

// ---------- Reactivate ----------
public sealed record ActivateUserCommand(int Id) : IRequest;

public sealed class ActivateUserHandler(IUserRepository users, UserGuard guard)
    : IRequestHandler<ActivateUserCommand, Unit>
{
    public async Task<Unit> Handle(ActivateUserCommand cmd, CancellationToken ct)
    {
        var user = await guard.FindAsync(cmd.Id, ct);
        user.Activate();
        await users.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
