using System.Diagnostics;
using Application.Exceptions;
using Application.Mediator;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Application.Behaviors;

/// <summary>Registra cada petición que pasa por el mediator con su duración y resultado.</summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next();
            logger.LogInformation("{Request} atendida en {Elapsed} ms", name, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex) when (ex is AppException or DomainException)
        {
            logger.LogWarning("{Request} rechazada en {Elapsed} ms: {Reason}", name, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Request} falló en {Elapsed} ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
