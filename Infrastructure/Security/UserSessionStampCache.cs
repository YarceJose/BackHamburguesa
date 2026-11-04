using System.Collections.Concurrent;
using Application.Abstractions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Security;

/// <summary>
/// Singleton: each user's stamp is read from the database once and then served from memory.
/// Handlers that change a stamp update the cache right after saving.
/// </summary>
public sealed class UserSessionStampCache(IServiceScopeFactory scopes) : IUserSessionStamps
{
    private readonly ConcurrentDictionary<int, string?> _stamps = new();

    public async ValueTask<string?> GetAsync(int userId, CancellationToken ct)
    {
        if (_stamps.TryGetValue(userId, out var cached)) return cached;

        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDBContext>();
        var stamp = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.SecurityStamp).FirstOrDefaultAsync(ct);
        _stamps[userId] = stamp;
        return stamp;
    }

    public void Set(int userId, string stamp) => _stamps[userId] = stamp;
}
