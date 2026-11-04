using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Mediator;

public sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    // One generic wrapper per request type: reflection happens only once.
    private static readonly ConcurrentDictionary<Type, RequestHandlerWrapper> Wrappers = new();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = (RequestHandlerWrapper<TResponse>)Wrappers.GetOrAdd(
            request.GetType(),
            static type => (RequestHandlerWrapper)Activator.CreateInstance(
                typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(type, typeof(TResponse)))!);

        return wrapper.Handle(request, serviceProvider, ct);
    }

    private abstract class RequestHandlerWrapper;

    private abstract class RequestHandlerWrapper<TResponse> : RequestHandlerWrapper
    {
        public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider sp, CancellationToken ct);
    }

    private sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public override Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider sp, CancellationToken ct)
        {
            var typed = (TRequest)request;
            var handler = sp.GetService<IRequestHandler<TRequest, TResponse>>()
                ?? throw new InvalidOperationException($"No handler registered for {typeof(TRequest).Name}.");

            RequestHandlerDelegate<TResponse> pipeline = () => handler.Handle(typed, ct);

            // The first registered behavior ends up outermost (runs first).
            foreach (var behavior in sp.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse())
            {
                var next = pipeline;
                pipeline = () => behavior.Handle(typed, next, ct);
            }

            return pipeline();
        }
    }
}
