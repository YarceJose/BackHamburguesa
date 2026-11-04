namespace Infrastructure.Security;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    /// <summary>HMAC key, at least 32 characters. In Docker it comes from the Jwt__Key variable.</summary>
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "BackHamburguesa";
    public string Audience { get; set; } = "Front-Hamburguesa";
    public int ExpirationMinutes { get; set; } = 480;
}
