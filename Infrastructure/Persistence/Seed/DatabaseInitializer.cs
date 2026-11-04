using Application.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Seed;

/// <summary>
/// Applies pending migrations, makes sure the store settings row exists, creates the first Admin if none exists
/// and, when <c>Seed:DemoData</c> is on, a starter menu and sector list the admin can edit.
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
            logger.LogInformation("Applying pending migrations...");
            await db.Database.MigrateAsync(ct);
        }

        var settings = await db.StoreSettings.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = StoreSettings.CreateDefault();
            db.StoreSettings.Add(settings);
            await db.SaveChangesAsync(ct);
        }
        // Tokens issued before the last "Cerrar jornada" stay invalid after a restart
        sp.GetRequiredService<ISessionRevocation>().Set(settings.StaffSessionsRevokedAt);

        await SeedAdminAsync(db, config, sp.GetRequiredService<IPasswordHasher>(), logger, ct);

        if (config.GetValue("Seed:DemoData", false))
            await SeedDemoDataAsync(db, logger, ct);
    }

    private static async Task SeedAdminAsync(AppDBContext db, IConfiguration config, IPasswordHasher hasher, ILogger logger, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Role == UserRole.Admin, ct))
            return;

        var username = config["Seed:Admin:Username"] ?? "admin";
        var password = config["Seed:Admin:Password"];
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No Admin exists and Seed:Admin:Password is missing: the initial admin was not created.");
            return;
        }

        var admin = User.CreateStaff(config["Seed:Admin:FullName"] ?? "Administrator", username, config["Seed:Admin:Email"],
            null, hasher.Hash(password), UserRole.Admin);
        db.Users.Add(admin);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Initial admin created: {Username}", admin.Username);
    }

    private static async Task SeedDemoDataAsync(AppDBContext db, ILogger logger, CancellationToken ct)
    {
        if (!await db.Sectors.AnyAsync(ct))
        {
            string[] sectors = ["Centro", "Parque principal", "Barrio La Esperanza", "Barrio El Progreso", "Vereda El Alto", "Vereda La Loma"];
            db.Sectors.AddRange(sectors.Select((name, i) => Sector.Create(name, i)));
            logger.LogInformation("Demo sectors created ({Count}); the admin can rename them.", sectors.Length);
        }

        if (!await db.Products.AnyAsync(ct))
        {
            db.Products.AddRange(
                Product.CreateMenuItem("Carne de Res", 19000, "carnes", "Corte a la parrilla con papas a la francesa y ensalada.", "https://i.pinimg.com/736x/1e/45/1d/1e451d29ceec70905007c4f9552b29a1.jpg"),
                Product.CreateMenuItem("Carne de Cerdo", 19000, "carnes", "Lomo de cerdo asado con papas y vegetales salteados.", "https://i.pinimg.com/736x/48/53/ce/4853cebd7448b719d7e4cf5f0f12f1db.jpg"),
                Product.CreateMenuItem("Pechuga", 19000, "carnes", "Pechuga a la plancha con papas y ensalada fresca.", "https://i.pinimg.com/736x/85/5e/ae/855eaeb368d8855ea1abf9713ce48497.jpg"),
                Product.CreateMenuItem("Costillas Ahumadas", 22000, "carnes", "Costillas en salsa BBQ ahumada con papas.", "https://i.pinimg.com/736x/75/85/6e/75856e3aaec4bb7ba3ce62f477b7229b.jpg"),
                Product.CreateMenuItem("Carne Mixta", 22000, "carnes", "Res, cerdo y pollo a la parrilla para compartir.", "https://i.pinimg.com/736x/99/0d/c2/990dc232b34f6effe8ebfc52b330c474.jpg"));
            logger.LogInformation("Demo menu created.");
        }

        await db.SaveChangesAsync(ct);
    }
}
