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
        // --- Request orchestrator ---
        services.AddScoped<IMediator, Mediator.Mediator>();

        // Pipeline: behaviors run in this order around every handler
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Every IRequestHandler<,> in this assembly
        var handlerInterface = typeof(IRequestHandler<,>);
        var handlers = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface)
                .Select(i => (Service: i, Implementation: t)));
        foreach (var (service, implementation) in handlers)
            services.AddScoped(service, implementation);

        services.AddScoped<UserGuard>();
        services.AddScoped<UserSessions>();
        services.AddScoped<Features.Store.StoreStatusReader>();

        // Product use case (not migrated to the mediator yet)
        services.AddScoped<CaseUseProduct>();
        return services;
    }
}
