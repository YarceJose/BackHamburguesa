using System.Collections.Concurrent;
using Application.Abstractions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Security;

/// <summary>
/// Singleton: each user's stamp is read from the database and kept in memory for a minute.
/// Handlers that change a stamp update the cache right after saving; changes made outside the API
/// (the reset-password console command) are picked up when the entry expires.
/// </summary>
public sealed class UserSessionStampCache(IServiceScopeFactory scopes, TimeProvider clock) : IUserSessionStamps
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(1);
    private readonly ConcurrentDictionary<int, (string? Stamp, DateTimeOffset LoadedAt)> _stamps = new();

    public async ValueTask<string?> GetAsync(int userId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        if (_stamps.TryGetValue(userId, out var cached) && now - cached.LoadedAt < Lifetime) return cached.Stamp;

        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDBContext>();
        var stamp = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.SecurityStamp).FirstOrDefaultAsync(ct);
        _stamps[userId] = (stamp, now);
        return stamp;
    }

    public void Set(int userId, string stamp) => _stamps[userId] = (stamp, clock.GetUtcNow());
}
