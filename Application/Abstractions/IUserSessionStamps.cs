namespace Application.Abstractions;

/// <summary>
/// Current <c>User.SecurityStamp</c> per user, cached in memory so every authenticated request can check
/// that its token is still valid without hitting the database each time.
/// </summary>
public interface IUserSessionStamps
{
    /// <summary>The current stamp, or null when the user does not exist.</summary>
    ValueTask<string?> GetAsync(int userId, CancellationToken ct);
    void Set(int userId, string stamp);
}
