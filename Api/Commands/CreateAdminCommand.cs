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
        var username = ConsoleInput.Option(args, "--username");
        var fullName = ConsoleInput.Option(args, "--name") ?? "Administrador";
        if (string.IsNullOrWhiteSpace(username))
        {
            Console.Error.WriteLine("Uso: create-admin --username <usuario> [--name \"Nombre completo\"]");
            return 2;
        }

        var password = ConsoleInput.ReadNewPassword();
        if (password is null) return 2;

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
}
