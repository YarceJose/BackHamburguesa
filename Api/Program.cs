using System.Text;
using System.Text.Json.Serialization;
using Api.Middleware;
using Api.Security;
using Application;
using Domain.Enums;
using Infrastructure;
using Infrastructure.Persistence.Seed;
using Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

public partial class Program
{
    private const string FrontCorsPolicy = "FrontHamburguesa";

    private static async Task Main(string[] args)
    {
        // Logger de arranque: captura errores que ocurran ANTES de que el host exista
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("BackHamburguesa API iniciando...");

            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File(
                        path: Path.Combine(
                            context.Configuration["Logging:FilePath"] ?? "logs",
                            "log-.txt"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        outputTemplate:
                            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} " +
                            "[{Level:u3}] {SourceContext} {Message:lj} " +
                            "{Properties:j}{NewLine}{Exception}");
            });

            // --- Servicios de la capa API ---
            builder.Services
                .AddControllers()
                // Los enums viajan como texto: "Admin", "Cook", "Delivery", "Customer"
                .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<AppExceptionHandler>();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "BackHamburguesa API",
                    Version = "v1",
                    Description =
                        "API de facturación electrónica y automatizaciones — comida rápida / DIAN"
                });

                // Botón "Authorize" para pegar el JWT que devuelve /api/auth/login
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT obtenido en POST /api/auth/login"
                });
                // API key para los endpoints de integración con n8n
                options.AddSecurityDefinition(ApiKeyAuthenticationHandler.SchemeName, new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = ApiKeyAuthenticationHandler.HeaderName,
                    Description = "API key de n8n (N8n:ApiKey)"
                });
                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
                    [new OpenApiSecuritySchemeReference(ApiKeyAuthenticationHandler.SchemeName, document)] = []
                });

                var xml = Path.Combine(AppContext.BaseDirectory, "Api.xml");
                if (File.Exists(xml)) options.IncludeXmlComments(xml);
            });

            // --- Capas (arquitectura hexagonal) ---
            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());

            // --- Autenticación: JWT para el personal, API key para n8n ---
            var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
            if (jwt.Key.Length < 32)
                throw new InvalidOperationException("Jwt:Key no configurada o con menos de 32 caracteres.");

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwt.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };
                })
                .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                    ApiKeyAuthenticationHandler.SchemeName,
                    o => o.ApiKey = builder.Configuration["N8n:ApiKey"] ?? string.Empty);

            builder.Services.AddAuthorizationBuilder()
                .AddPolicy(Policies.AdminOnly, p => p
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireRole(nameof(UserRole.Admin)))
                .AddPolicy(Policies.Staff, p => p
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireRole(Policies.StaffRoles))
                .AddPolicy(Policies.N8n, p => p
                    .AddAuthenticationSchemes(ApiKeyAuthenticationHandler.SchemeName)
                    .RequireAuthenticatedUser());

            // --- CORS para Front-Hamburguesa ---
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            builder.Services.AddCors(options =>
                options.AddPolicy(FrontCorsPolicy, policy => policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()));

            var app = builder.Build();

            // Migraciones + Admin inicial (los usuarios solo los crea un Admin)
            await DatabaseInitializer.InitializeAsync(app.Services);

            // --- Pipeline (el orden importa) ---
            // El exception handler va primero para que el log de requests
            // vea la excepción y la registre con el contexto de la petición.
            app.UseExceptionHandler();
            app.UseSerilogRequestLogging();

            // Swagger: siempre en Development, o en otros entornos si Swagger:Enabled = true
            var enableSwagger = app.Environment.IsDevelopment()
                                || app.Configuration.GetValue<bool>("Swagger:Enabled");

            if (enableSwagger)
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "BackHamburguesa API v1");
                    options.RoutePrefix = "swagger";
                });
            }

            app.UseCors(FrontCorsPolicy);
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHealthChecks("/health");
            app.MapControllers();

            await app.RunAsync();
        }
        catch (Exception ex) when (ex is not HostAbortedException)
        {
            Log.Fatal(ex, "La API terminó de forma inesperada");
            Environment.ExitCode = 1; // para que Docker/CI se enteren del fallo
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
