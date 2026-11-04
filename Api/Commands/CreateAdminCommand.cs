using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Ports;

namespace Api.Commands;

/// <summary>
/// One-time console command to create an administrator directly in the database:
///   docker compose exec -it api dotnet Api.dll create-admin --username admin --name "Administrador"
/// The password is typed twice without being shown (or piped through stdin). Nothing is read from configuration.
/// </summary>
public static class CreateAdminCommand
{
    public const string Name = "create-admin";

    public static async Task<int> RunAsync(IServiceProvider services, string[] args)
    {
        var username = Option(args, "--username");
        var fullName = Option(args, "--name") ?? "Administrador";
        if (string.IsNullOrWhiteSpace(username))
        {
            Console.Error.WriteLine("Uso: create-admin --username <usuario> [--name \"Nombre completo\"]");
            return 2;
        }

        var password = ReadPassword("Contraseña (mínimo 8 caracteres): ");
        if (password.Length < 8)
        {
            Console.Error.WriteLine("La contraseña debe tener al menos 8 caracteres.");
            return 2;
        }
        if (!Console.IsInputRedirected && ReadPassword("Repite la contraseña: ") != password)
        {
            Console.Error.WriteLine("Las contraseñas no coinciden.");
            return 2;
        }

        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        try
        {
            var normalized = User.NormalizeUsername(username);
            if (await users.UsernameExistsAsync(normalized, null, default))
            {
                Console.Error.WriteLine($"Ya existe un usuario '{normalized}'. Cambia su contraseña desde el panel.");
                return 1;
            }

            var admin = User.CreateStaff(fullName, normalized, null, null, hasher.Hash(password), UserRole.Admin);
            await users.AddAsync(admin, default);
            await users.SaveChangesAsync(default);
            Console.WriteLine($"Administrador '{admin.Username}' creado.");
            return 0;
        }
        catch (DomainException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }

    private static string? Option(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    /// <summary>Reads a line without echoing it; falls back to a plain line when input is piped.</summary>
    private static string ReadPassword(string prompt)
    {
        if (Console.IsInputRedirected) return Console.ReadLine() ?? string.Empty;

        Console.Write(prompt);
        var chars = new List<char>();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace) { if (chars.Count > 0) chars.RemoveAt(chars.Count - 1); continue; }
            if (!char.IsControl(key.KeyChar)) chars.Add(key.KeyChar);
        }
        Console.WriteLine();
        return new string(chars.ToArray());
    }
}
