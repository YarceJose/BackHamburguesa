using Application.DTOs.Users;
using Application.Mediator;
using Domain.Enums;
using Domain.Interfaces;

namespace Application.Features.Users;

// ---------- List ----------
public sealed record GetUsersQuery(UserRole? Role, bool IncludeInactive) : IRequest<List<UserResponse>>;

public sealed class GetUsersHandler(IUserRepository users) : IRequestHandler<GetUsersQuery, List<UserResponse>>
{
    public async Task<List<UserResponse>> Handle(GetUsersQuery q, CancellationToken ct) =>
        (await users.GetAllAsync(q.Role, q.IncludeInactive, ct)).Select(UserResponse.From).ToList();
}

// ---------- By id ----------
public sealed record GetUserByIdQuery(int Id) : IRequest<UserResponse>;

public sealed class GetUserByIdHandler(UserGuard guard) : IRequestHandler<GetUserByIdQuery, UserResponse>
{
    public async Task<UserResponse> Handle(GetUserByIdQuery q, CancellationToken ct) =>
        UserResponse.From(await guard.FindAsync(q.Id, ct));
}
