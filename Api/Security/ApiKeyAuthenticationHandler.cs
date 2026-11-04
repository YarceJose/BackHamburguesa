using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Api.Security;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public string ApiKey { get; set; } = string.Empty;
}

/// <summary>
/// Autenticación máquina-a-máquina para n8n: envía la cabecera X-Api-Key.
/// Se usa solo en los endpoints de integración, no en el front.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    public const string SchemeName = "N8nApiKey";
    public const string HeaderName = "X-Api-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var provided) || string.IsNullOrEmpty(provided))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (string.IsNullOrEmpty(Options.ApiKey) || !FixedTimeEquals(provided.ToString(), Options.ApiKey))
            return Task.FromResult(AuthenticateResult.Fail("API key inválida."));

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "n8n")], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
