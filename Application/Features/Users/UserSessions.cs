using Application.Abstractions;
using Domain.Entities;

namespace Application.Features.Users;

/// <summary>
/// Call after saving a user whose security stamp changed: refreshes the cache (their tokens stop working
/// right away) and tells their open screens to sign out.
/// </summary>
public sealed class UserSessions(IUserSessionStamps stamps, IOrderEvents events)
{
    public async Task RevokeIfChangedAsync(User user, string previousStamp, CancellationToken ct)
    {
        if (user.SecurityStamp == previousStamp) return;
        stamps.Set(user.Id, user.SecurityStamp);
        await events.UserSessionRevokedAsync(user.Id, ct);
    }
}
