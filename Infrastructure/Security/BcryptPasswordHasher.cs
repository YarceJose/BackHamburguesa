using Domain.Ports;

namespace Infrastructure.Security;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string password) => BCrypt.Net.BCrypt.EnhancedHashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.EnhancedVerify(password, passwordHash);
}
