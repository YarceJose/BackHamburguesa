namespace Application.Mediator;

/// <summary>Respuesta vacía para comandos que no devuelven nada.</summary>
public readonly record struct Unit
{
    public static readonly Unit Value = new();
}

/// <summary>Petición (comando o consulta) que produce un <typeparamref name="TResponse"/>.</summary>
public interface IRequest<TResponse>;

/// <summary>Comando sin valor de retorno.</summary>
public interface IRequest : IRequest<Unit>;

/// <summary>Atiende una petición concreta. Hay exactamente un handler por petición.</summary>
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken ct);
}

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Paso del pipeline que envuelve a todos los handlers (logging, validación, transacciones...).
/// Se ejecutan en el orden en que se registran.
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct);
}

/// <summary>Orquestador: recibe la petición, la pasa por el pipeline y la entrega a su handler.</summary>
public interface IMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
}
