using Application.Exceptions;
using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Middleware;

/// <summary>
/// Turns domain/application exceptions into ProblemDetails responses.
/// Every response carries a stable "code" (see Domain.Errors.ErrorCodes) that the front translates.
/// </summary>
public sealed class AppExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title, code) = exception switch
        {
            ValidationException e => (StatusCodes.Status400BadRequest, "Invalid data", e.Code),
            NotFoundException e => (StatusCodes.Status404NotFound, "Not found", e.Code),
            ConflictException e => (StatusCodes.Status409Conflict, "Conflict", e.Code),
            InvalidCredentialsException e => (StatusCodes.Status401Unauthorized, "Unauthorized", e.Code),
            ForbiddenException e => (StatusCodes.Status403Forbidden, "Forbidden", e.Code),
            BusinessRuleException e => (StatusCodes.Status422UnprocessableEntity, "Business rule violated", e.Code),
            DomainException e => (StatusCodes.Status422UnprocessableEntity, "Business rule violated", e.Code),
            _ => (0, null, null)
        };

        if (status == 0) return false; // handled by the default handler (500)

        var problem = exception is ValidationException validation
            ? new ValidationProblemDetails(validation.Errors)
            : new ProblemDetails();
        problem.Status = status;
        problem.Title = title;
        problem.Detail = exception.Message;
        problem.Extensions["code"] = code;

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = problem
        });
    }
}
