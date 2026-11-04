using Application.DTOs.Users;
using Application.Exceptions;
using Application.Features.Users;
using Application.Mediator;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Features.Customers;

// ---------- Buscar cliente por teléfono (n8n) ----------
public sealed record GetCustomerByPhoneQuery(string Phone) : IRequest<UserResponse>;

public sealed class GetCustomerByPhoneHandler(IUserRepository users) : IRequestHandler<GetCustomerByPhoneQuery, UserResponse>
{
    public async Task<UserResponse> Handle(GetCustomerByPhoneQuery q, CancellationToken ct)
    {
        var customer = await users.GetCustomerByPhoneAsync(q.Phone.Trim(), ct)
            ?? throw new NotFoundException("No existe un cliente con ese teléfono.");
        return UserResponse.From(customer);
    }
}

// ---------- Crear o actualizar cliente por teléfono (n8n) ----------
public sealed record UpsertCustomerResult(UserResponse Customer, bool Created);

public sealed record UpsertCustomerCommand(UpsertCustomerRequest Data) : IRequest<UpsertCustomerResult>;

public sealed class UpsertCustomerHandler(IUserRepository users, UserGuard guard)
    : IRequestHandler<UpsertCustomerCommand, UpsertCustomerResult>
{
    public async Task<UpsertCustomerResult> Handle(UpsertCustomerCommand cmd, CancellationToken ct)
    {
        var r = cmd.Data;
        var phone = r.Phone.Trim();
        var existing = await users.GetCustomerByPhoneAsync(phone, ct);

        if (!string.IsNullOrWhiteSpace(r.Email))
            await guard.EnsureEmailIsFreeAsync(r.Email, existing?.Id, ct);

        if (existing is not null)
        {
            existing.UpdateProfile(r.FullName, r.Email ?? existing.Email, phone);
            existing.Activate();
            await users.SaveChangesAsync(ct);
            return new UpsertCustomerResult(UserResponse.From(existing), Created: false);
        }

        var customer = User.CreateCustomer(r.FullName, phone, r.Email);
        await users.AddAsync(customer, ct);
        await users.SaveChangesAsync(ct);
        return new UpsertCustomerResult(UserResponse.From(customer), Created: true);
    }
}
