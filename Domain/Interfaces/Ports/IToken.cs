using Domain.Entities;

namespace Domain.Ports;

public sealed record AccessToken(string Token, DateTime ExpiresAt);

/// <summary>Port that issues access tokens (implemented with JWT in Infrastructure).</summary>
public interface IToken
{
    AccessToken GenerateToken(User user);
}
