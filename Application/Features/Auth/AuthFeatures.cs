using Application.DTOs.Auth;
using Application.DTOs.Users;
using Application.Exceptions;
using Application.Features.Users;
using Application.Mediator;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Ports;
using Domain.Errors;

namespace Application.Features.Auth;

// ---------- Login: admin, cook and delivery with username + password; customers never sign in ----------
public sealed record LoginCommand(LoginRequest Data) : IRequest<LoginResponse>;

public sealed class LoginHandler(IUserRepository users, IPasswordHasher hasher, IToken tokens)
    : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var username = cmd.Data.Username.Trim().ToLowerInvariant();
        var user = await users.GetByUsernameAsync(username, ct);

        // Same error for "not found", "customer", "inactive" or "wrong password": no information leaks.
        if (user is null || !user.CanLogin || !hasher.Verify(cmd.Data.Password, user.PasswordHash!))
            throw new InvalidCredentialsException();

        user.RegisterLogin();
        await users.SaveChangesAsync(ct);

        var token = tokens.GenerateToken(user);
        return new LoginResponse(token.Token, token.ExpiresAt, UserResponse.From(user));
    }
}

// ---------- Current user profile ----------
public sealed record GetProfileQuery(int UserId) : IRequest<UserResponse>;

public sealed class GetProfileHandler(UserGuard guard) : IRequestHandler<GetProfileQuery, UserResponse>
{
    public async Task<UserResponse> Handle(GetProfileQuery q, CancellationToken ct) =>
        UserResponse.From(await guard.FindAsync(q.UserId, ct));
}

// ---------- Change own password ----------
public sealed record ChangeOwnPasswordCommand(int UserId, ChangePasswordRequest Data) : IRequest;

public sealed class ChangeOwnPasswordHandler(IUserRepository users, IPasswordHasher hasher, UserGuard guard, UserSessions sessions)
    : IRequestHandler<ChangeOwnPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ChangeOwnPasswordCommand cmd, CancellationToken ct)
    {
        var user = await guard.FindAsync(cmd.UserId, ct);

        if (!user.CanLogin || !hasher.Verify(cmd.Data.CurrentPassword, user.PasswordHash!))
            throw new BusinessRuleException(ErrorCodes.WrongCurrentPassword, "The current password is incorrect.");

        var stamp = user.SecurityStamp;
        user.ChangePassword(hasher.Hash(cmd.Data.NewPassword));
        await users.SaveChangesAsync(ct);
        await sessions.RevokeIfChangedAsync(user, stamp, ct); // other devices sign out; this one signs in again
        return Unit.Value;
    }
}
