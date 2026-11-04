using Domain.Interfaces;
using Domain.Ports;
using Application.Abstractions;
using Infrastructure.Notifications;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Security;
using Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        // Read here (not inside the lambda) so a missing value fails at startup,
        // not the first time someone asks for the DbContext.
        // Must match ConnectionStrings__Default in docker-compose.
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddDbContext<AppDBContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null));

            if (isDevelopment)
                options.EnableSensitiveDataLogging();
        });

        services.AddHealthChecks().AddDbContextCheck<AppDBContext>("database");

        // Repositories (driven adapters)
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ISectorRepository, SectorRepository>();
        services.AddScoped<IStoreSettingsRepository, StoreSettingsRepository>();
        services.AddScoped<IPaymentAccountRepository, PaymentAccountRepository>();

        // Clock (injectable so time rules can be tested)
        services.AddSingleton(TimeProvider.System);

        // Uploads (payment receipts, QR images)
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.Section));
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        // n8n (WhatsApp notifications)
        services.Configure<N8nOptions>(configuration.GetSection(N8nOptions.Section));
        services.AddHttpClient<N8nWebhook>();

        // Security
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.Section))
            .Validate(o => o.Key.Length >= 32, "Jwt:Key must be at least 32 characters.")
            .ValidateOnStart();
        services.AddSingleton<IToken, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ISessionRevocation, SessionRevocationCache>();
        services.AddSingleton<IUserSessionStamps, UserSessionStampCache>();

        return services;
    }
}
