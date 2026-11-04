using Application.Abstractions;

namespace Infrastructure.Security;

/// <summary>Singleton: loaded at startup from StoreSettings and updated when the admin closes the day.</summary>
public sealed class SessionRevocationCache : ISessionRevocation
{
    private long _ticks; // 0 = never revoked

    public DateTime? StaffSessionsRevokedAt
    {
        get
        {
            var ticks = Interlocked.Read(ref _ticks);
            return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
        }
    }

    public void Set(DateTime? revokedAt) => Interlocked.Exchange(ref _ticks, revokedAt?.Ticks ?? 0);
}
