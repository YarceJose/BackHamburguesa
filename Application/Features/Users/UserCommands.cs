using Application.DTOs.Users;
using Application.Exceptions;
using Application.Mediator;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Domain.Ports;

namespace Application.Features.Users;

// ---------- Crear ----------
/// <summary>Admin/Cook/Delivery requieren correo y contraseña. Customer requiere teléfono y no lleva contraseña.</summary>
public sealed record CreateUserCommand(CreateUserRequest Data) : IRequest<UserResponse>;

public sealed class CreateUserHandler(IUserRepository users, IPasswordHasher hasher, UserGuard guard)
    : IRequestHandler<CreateUserCommand, UserResponse>
{
    public async Task<UserResponse> Handle(CreateUserCommand cmd, CancellationToken ct)
    {
        var r = cmd.Data;
        User user;

        if (r.Role.IsStaff())
        {
            if (string.IsNullOrWhiteSpace(r.Email))
                throw new BusinessRuleException("El correo es obligatorio para Admin, Cook y Delivery.");
            if (string.IsNullOrWhiteSpace(r.Password))
                throw new BusinessRuleException("La contraseña es obligatoria para Admin, Cook y Delivery.");

            await guard.EnsureEmailIsFreeAsync(r.Email, null, ct);
            user = User.CreateStaff(r.FullName, r.Email, r.Phone, hasher.Hash(r.Password), r.Role);
        }
        else
        {
            if (!string.IsNullOrEmpty(r.Password))
                throw new BusinessRuleException("Los clientes no tienen contraseña: no se registran ni inician sesión.");
            if (string.IsNullOrWhiteSpace(r.Phone))
                throw new BusinessRuleException("El teléfono es obligatorio para un cliente.");
            if (await users.GetCustomerByPhoneAsync(r.Phone.Trim(), ct) is not null)
                throw new ConflictException("Ya existe un cliente con ese teléfono.");
            if (!string.IsNullOrWhiteSpace(r.Email))
                await guard.EnsureEmailIsFreeAsync(r.Email, null, ct);

            user = User.CreateCustomer(r.FullName, r.Phone, r.Email);
        }

        await users.AddAsync(user, ct);
        await users.SaveChangesAsync(ct);
        return UserResponse.From(user);
    }
}

// ---------- Editar ----------
public sealed record UpdateUserCommand(int Id, int CurrentUserId, UpdateUserRequest Data) : IRequest<UserResponse>;

public sealed class UpdateUserHandler(IUserRepository users, UserGuard guard)
    : IRequestHandler<UpdateUserCommand, UserResponse>
{
    public async Task<UserResponse> Handle(UpdateUserCommand cmd, CancellationToken ct)
    {
        var r = cmd.Data;
        var user = await guard.FindAsync(cmd.Id, ct);

        if (!string.IsNullOrWhiteSpace(r.Email))
            await guard.EnsureEmailIsFreeAsync(r.Email, user.Id, ct);

        if (user.Role == UserRole.Admin && r.Role != UserRole.Admin)
        {
            if (user.Id == cmd.CurrentUserId)
                throw new BusinessRuleException("No puedes quitarte el rol de Admin a ti mismo.");
            await guard.EnsureNotLastAdminAsync(user, ct);
        }

        user.UpdateProfile(r.FullName, r.Email, r.Phone);
        user.ChangeRole(r.Role);

        await users.SaveChangesAsync(ct);
        return UserResponse.From(user);
    }
}

// ---------- Contraseña (la asigna el Admin) ----------
public sealed record ResetPasswordCommand(int Id, ResetPasswordRequest Data) : IRequest;

public sealed class ResetPasswordHandler(IUserRepository users, IPasswordHasher hasher, UserGuard guard)
    : IRequestHandler<ResetPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ResetPasswordCommand cmd, CancellationToken ct)
    {
        var user = await guard.FindAsync(cmd.Id, ct);
        user.ChangePassword(hasher.Hash(cmd.Data.NewPassword));
        await users.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ---------- Desactivar (borrado lógico) ----------
public sealed record DeactivateUserCommand(int Id, int CurrentUserId) : IRequest;

public sealed class DeactivateUserHandler(IUserRepository users, UserGuard guard)
    : IRequestHandler<DeactivateUserCommand, Unit>
{
    public async Task<Unit> Handle(DeactivateUserCommand cmd, CancellationToken ct)
    {
        var user = await guard.FindAsync(cmd.Id, ct);

        if (user.Id == cmd.CurrentUserId)
            throw new BusinessRuleException("No puedes desactivar tu propio usuario.");
        await guard.EnsureNotLastAdminAsync(user, ct);

        user.Deactivate();
        await users.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ---------- Reactivar ----------
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
