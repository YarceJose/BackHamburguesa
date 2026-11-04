using Application.DTOs.Auth;
using Application.DTOs.Users;
using Application.Exceptions;
using Application.Features.Users;
using Application.Mediator;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Ports;

namespace Application.Features.Auth;

// ---------- Login (personal desde Front-Hamburguesa; los clientes no inician sesión) ----------
public sealed record LoginCommand(LoginRequest Data) : IRequest<LoginResponse>;

public sealed class LoginHandler(IUserRepository users, IPasswordHasher hasher, IToken tokens)
    : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(User.NormalizeEmail(cmd.Data.Email), ct);

        // Mismo error para "no existe", "es cliente", "inactivo" o "contraseña mala": no se filtra información.
        if (user is null || !user.CanLogin || !hasher.Verify(cmd.Data.Password, user.PasswordHash!))
            throw new InvalidCredentialsException();

        user.RegisterLogin();
        await users.SaveChangesAsync(ct);

        var token = tokens.GenerateToken(user);
        return new LoginResponse(token.Token, token.ExpiresAt, UserResponse.From(user));
    }
}

// ---------- Mi perfil ----------
public sealed record GetProfileQuery(int UserId) : IRequest<UserResponse>;

public sealed class GetProfileHandler(UserGuard guard) : IRequestHandler<GetProfileQuery, UserResponse>
{
    public async Task<UserResponse> Handle(GetProfileQuery q, CancellationToken ct) =>
        UserResponse.From(await guard.FindAsync(q.UserId, ct));
}

// ---------- Cambiar mi contraseña ----------
public sealed record ChangeOwnPasswordCommand(int UserId, ChangePasswordRequest Data) : IRequest;

public sealed class ChangeOwnPasswordHandler(IUserRepository users, IPasswordHasher hasher, UserGuard guard)
    : IRequestHandler<ChangeOwnPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ChangeOwnPasswordCommand cmd, CancellationToken ct)
    {
        var user = await guard.FindAsync(cmd.UserId, ct);

        if (!user.CanLogin || !hasher.Verify(cmd.Data.CurrentPassword, user.PasswordHash!))
            throw new BusinessRuleException("La contraseña actual no es correcta.");

        user.ChangePassword(hasher.Hash(cmd.Data.NewPassword));
        await users.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
