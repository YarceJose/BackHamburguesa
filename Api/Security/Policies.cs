using System.Security.Claims;
using Domain.Enums;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Api.Security;

public static class Policies
{
    public const string AdminOnly = nameof(AdminOnly);
    public const string Staff = nameof(Staff);       // Admin, Cook, Delivery
    public const string N8n = nameof(N8n);

    public static readonly string[] StaffRoles =
        [nameof(UserRole.Admin), nameof(UserRole.Cook), nameof(UserRole.Delivery)];

    public static int GetUserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new UnauthorizedAccessException("Token sin identificador de usuario.");
}
