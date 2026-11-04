using Domain.Entities;
using Domain.Interfaces;
using Domain.Ports;

namespace Api.Commands;

/// <summary>
/// One-time console command to set a new password for any staff user (admin, cook or delivery),
/// for when nobody can sign in to change it from the panel:
///   docker compose exec -it api dotnet Api.dll reset-password --username admin
/// The password is typed twice without being shown. Only its bcrypt hash is saved, and the user's
/// open sessions are closed (the running API notices within a minute).
/// </summary>
public static class ResetPasswordCommand
{
    public const string Name = "reset-password";

    public static async Task<int> RunAsync(IServiceProvider services, string[] args)
    {
        var username = ConsoleInput.Option(args, "--username");
        if (string.IsNullOrWhiteSpace(username))
        {
            Console.Error.WriteLine("Uso: reset-password --username <usuario>");
            return 2;
        }

        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = await users.GetByUsernameAsync(User.NormalizeUsername(username), default);
        if (user is null)
        {
            Console.Error.WriteLine($"No existe el usuario '{username.Trim().ToLowerInvariant()}'.");
            return 1;
        }

        var password = ConsoleInput.ReadNewPassword();
        if (password is null) return 2;

        user.ChangePassword(hasher.Hash(password));
        await users.SaveChangesAsync(default);

        Console.WriteLine($"Contraseña de '{user.Username}' actualizada. Sus sesiones abiertas se cerrarán.");
        if (!user.CanLogin)
            Console.WriteLine("Ojo: este usuario está desactivado. Actívalo desde el panel para que pueda ingresar.");
        return 0;
    }
}
