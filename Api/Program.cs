using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Api.Middleware;
using Api.Realtime;
using Api.Workers;
using Api.Security;
using Application;
using Application.Abstractions;
using Domain.Enums;
using Domain.Errors;
using Domain.Services;
using Infrastructure;
using Infrastructure.Persistence.Seed;
using Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

public partial class Program
{
    private const string FrontCorsPolicy = "FrontHamburguesa";

    private static async Task Main(string[] args)
    {
        // Bootstrap logger: captures errors that happen BEFORE the host exists
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("BackHamburguesa API starting...");

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

            // --- API layer services ---
            builder.Services
                .AddControllers()
                // Enums travel as text: "Admin", "Cook", "Delivery", "Customer"
                .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
            {
                // Automatic [ApiController] 400s also get a stable code for the front
                if (ctx.ProblemDetails is ValidationProblemDetails)
                    ctx.ProblemDetails.Extensions.TryAdd("code", ErrorCodes.ValidationFailed);
            });
            builder.Services.AddExceptionHandler<AppExceptionHandler>();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "BackHamburguesa API",
                    Version = "v1",
                    Description =
                        "Fast food store API: staff, customers, orders, automations (n8n) and e-invoicing (DIAN)"
                });

                // "Authorize" button to paste the JWT returned by the login endpoints
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT from POST /api/auth/login"
                });
                // API key for the n8n integration endpoints
                options.AddSecurityDefinition(ApiKeyAuthenticationHandler.SchemeName, new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = ApiKeyAuthenticationHandler.HeaderName,
                    Description = "n8n API key (N8n:ApiKey)"
                });
                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
                    [new OpenApiSecuritySchemeReference(ApiKeyAuthenticationHandler.SchemeName, document)] = []
                });

                var xml = Path.Combine(AppContext.BaseDirectory, "Api.xml");
                if (File.Exists(xml)) options.IncludeXmlComments(xml);
            });

            // --- Layers (hexagonal architecture) ---
            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());

            // --- Real time (staff screens) + order notifications + background alerts ---
            builder.Services.AddSignalR()
                .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            builder.Services.AddScoped<IOrderEvents, OrderEventsPublisher>();
            builder.Services.AddHostedService<OrderMaintenanceWorker>();

            // --- Authentication: JWT for staff, API key for n8n ---
            var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
            if (jwt.Key.Length < 32)
                throw new InvalidOperationException("Jwt:Key is missing or shorter than 32 characters.");

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
                    options.Events = new JwtBearerEvents
                    {
                        // Browsers cannot send headers on WebSockets: SignalR passes the token in the query string
                        OnMessageReceived = context =>
                        {
                            var token = context.Request.Query["access_token"];
                            if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments(OrdersHub.Path))
                                context.Token = token;
                            return Task.CompletedTask;
                        },
                        // "Cerrar jornada" invalidates kitchen and delivery tokens issued before it (admin is never affected)
                        OnTokenValidated = context =>
                        {
                            var revocation = context.HttpContext.RequestServices.GetRequiredService<ISessionRevocation>();
                            if (context.SecurityToken is JsonWebToken token
                                && Enum.TryParse<UserRole>(context.Principal?.FindFirst("role")?.Value, out var role)
                                && OrderTiming.IsSessionRevoked(role, token.IssuedAt, revocation.StaffSessionsRevokedAt))
                            {
                                context.Fail("Session closed by the administrator.");
                            }
                            return Task.CompletedTask;
                        }
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
                .AddPolicy(Policies.Kitchen, p => p
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireRole(nameof(UserRole.Admin), nameof(UserRole.Cook)))
                .AddPolicy(Policies.Dispatch, p => p
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireRole(nameof(UserRole.Admin), nameof(UserRole.Delivery)))
                .AddPolicy(Policies.N8n, p => p
                    .AddAuthenticationSchemes(ApiKeyAuthenticationHandler.SchemeName)
                    .RequireAuthenticatedUser());

            // --- Rate limiting: sign-in and public orders, per IP ---
            builder.Services.AddRateLimiter(o =>
            {
                o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                foreach (var policy in new[] { RateLimits.Auth, RateLimits.Orders })
                {
                    o.AddPolicy(policy, ctx => RateLimitPartition.GetFixedWindowLimiter(
                        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
                }
            });

            // --- CORS for Front-Hamburguesa ---
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            builder.Services.AddCors(options =>
                options.AddPolicy(FrontCorsPolicy, policy => policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials())); // SignalR negotiation

            var app = builder.Build();

            // Migrations + initial Admin (only an Admin can create users)
            await DatabaseInitializer.InitializeAsync(app.Services);

            // --- Pipeline (order matters) ---
            // The exception handler goes first so request logging
            // sees the exception and logs it with the request context.
            app.UseExceptionHandler();
            app.UseSerilogRequestLogging();

            // Swagger: always in Development, elsewhere only if Swagger:Enabled = true
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
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHealthChecks("/health");
            app.MapControllers();
            app.MapHub<OrdersHub>(OrdersHub.Path);

            await app.RunAsync();
        }
        catch (Exception ex) when (ex is not HostAbortedException)
        {
            Log.Fatal(ex, "The API terminated unexpectedly");
            Environment.ExitCode = 1; // so Docker/CI notice the failure
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
