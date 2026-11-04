using Application.Exceptions;
using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Middleware;

/// <summary>Traduce las excepciones de dominio/aplicación a respuestas ProblemDetails.</summary>
public sealed class AppExceptionHandler(IProblemDetailsService problemDetails)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is ValidationException validation)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return await problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                Exception = exception,
                ProblemDetails = new ValidationProblemDetails(validation.Errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Datos inválidos",
                    Detail = exception.Message
                }
            });
        }

        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflicto"),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "No autorizado"),
            BusinessRuleException or DomainException => (StatusCodes.Status422UnprocessableEntity, "Regla de negocio"),
            _ => (0, null)
        };

        if (status == 0) return false; // lo maneja el handler por defecto (500)

        context.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = exception.Message }
        });
    }
}
