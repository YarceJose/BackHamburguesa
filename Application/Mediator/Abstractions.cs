namespace Application.Mediator;

/// <summary>Empty response for commands that return nothing.</summary>
public readonly record struct Unit
{
    public static readonly Unit Value = new();
}

/// <summary>Request (command or query) that produces a <typeparamref name="TResponse"/>.</summary>
public interface IRequest<TResponse>;

/// <summary>Command with no return value.</summary>
public interface IRequest : IRequest<Unit>;

/// <summary>Handles one specific request. There is exactly one handler per request.</summary>
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken ct);
}

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Pipeline step that wraps every handler (logging, validation, transactions...).
/// Behaviors run in registration order.
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct);
}

/// <summary>Orchestrator: receives a request, runs it through the pipeline and hands it to its handler.</summary>
public interface IMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
}
