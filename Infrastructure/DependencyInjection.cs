using Domain.Interfaces;
using Domain.Ports;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        // La clave se lee aquí (no dentro del lambda) para fallar al arrancar,
        // y no la primera vez que alguien pida el DbContext.
        // Debe coincidir con ConnectionStrings__Default del docker-compose.
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' no configurada.");

        services.AddDbContext<AppDBContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null));

            if (isDevelopment)
                options.EnableSensitiveDataLogging();
        });

        services.AddHealthChecks().AddDbContextCheck<AppDBContext>("database");

        // Repositorios (adaptadores de salida)
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Seguridad
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.Section))
            .Validate(o => o.Key.Length >= 32, "Jwt:Key debe tener al menos 32 caracteres.")
            .ValidateOnStart();
        services.AddSingleton<IToken, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        return services;
    }
}
