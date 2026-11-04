namespace Application.Abstractions;

/// <summary>
/// In-memory copy of <c>StoreSettings.StaffSessionsRevokedAt</c>, read on every authenticated request
/// without hitting the database. Updated when the admin closes the day.
/// </summary>
public interface ISessionRevocation
{
    DateTime? StaffSessionsRevokedAt { get; }
    void Set(DateTime? revokedAt);
}
