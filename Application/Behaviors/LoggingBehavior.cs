using System.Diagnostics;
using Application.Exceptions;
using Application.Mediator;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Application.Behaviors;

/// <summary>Logs every request that goes through the mediator with its duration and outcome.</summary>
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
            logger.LogInformation("{Request} handled in {Elapsed} ms", name, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex) when (ex is AppException or DomainException)
        {
            logger.LogWarning("{Request} rejected in {Elapsed} ms: {Reason}", name, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Request} failed in {Elapsed} ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
