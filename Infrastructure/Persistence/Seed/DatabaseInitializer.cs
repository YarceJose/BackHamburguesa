using Domain.Entities;
using Domain.Enums;
using Domain.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Seed;

/// <summary>
/// Aplica migraciones pendientes y crea el primer Admin si no existe ninguno.
/// Como los usuarios solo los crea un Admin, sin este seed nadie podría entrar al sistema.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDBContext>();
        var config = sp.GetRequiredService<IConfiguration>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseInitializer));

        if (config.GetValue("Database:MigrateOnStartup", true))
        {
            logger.LogInformation("Aplicando migraciones pendientes...");
            await db.Database.MigrateAsync(ct);
        }

        if (await db.Users.AnyAsync(u => u.Role == UserRole.Admin, ct))
            return;

        var email = config["Seed:Admin:Email"];
        var password = config["Seed:Admin:Password"];
        var fullName = config["Seed:Admin:FullName"] ?? "Administrador";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No hay ningún Admin y faltan Seed:Admin:Email / Seed:Admin:Password: no se creó el admin inicial.");
            return;
        }

        var hasher = sp.GetRequiredService<IPasswordHasher>();
        db.Users.Add(User.CreateStaff(fullName, email, null, hasher.Hash(password), UserRole.Admin));
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Admin inicial creado: {Email}", User.NormalizeEmail(email));
    }
}
