namespace Api.Security;

public static class RateLimits
{
    /// <summary>Sign-in: 10 attempts per minute per IP (shared by every staff member behind the same router).</summary>
    public const string Auth = "auth";

    /// <summary>Public orders and receipt uploads: 10 per minute per IP.</summary>
    public const string Orders = "orders";
}
