using Domain.Entities;

namespace Domain.Ports;

public sealed record AccessToken(string Token, DateTime ExpiresAt);

/// <summary>Puerto para emitir tokens de acceso (implementado con JWT en Infrastructure).</summary>
public interface IToken
{
    AccessToken GenerateToken(User user);
}
