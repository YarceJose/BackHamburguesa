namespace Infrastructure.Security;

public static class SessionClaims
{
    /// <summary>JWT claim with the user's security stamp at sign-in time.</summary>
    public const string Stamp = "stamp";
}
