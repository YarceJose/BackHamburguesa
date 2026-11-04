using Application.Behaviors;
using Application.Features.Users;
using Application.Mediator;
using Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // --- Orquestador de peticiones ---
        services.AddScoped<IMediator, Mediator.Mediator>();

        // Pipeline: se ejecutan en este orden alrededor de cada handler
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Todos los IRequestHandler<,> de este ensamblado
        var handlerInterface = typeof(IRequestHandler<,>);
        var handlers = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface)
                .Select(i => (Service: i, Implementation: t)));
        foreach (var (service, implementation) in handlers)
            services.AddScoped(service, implementation);

        services.AddScoped<UserGuard>();

        // Caso de uso de productos (aún sin migrar al mediator)
        services.AddScoped<CaseUseProduct>();
        return services;
    }
}
