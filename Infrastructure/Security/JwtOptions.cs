namespace Infrastructure.Security;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    /// <summary>Clave HMAC de al menos 32 caracteres. En Docker llega por la variable Jwt__Key.</summary>
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "BackHamburguesa";
    public string Audience { get; set; } = "Front-Hamburguesa";
    public int ExpirationMinutes { get; set; } = 480;
}
