using System.ComponentModel.DataAnnotations;
using Application.Mediator;
using AppValidationException = Application.Exceptions.ValidationException;

namespace Application.Behaviors;

/// <summary>
/// Validates the request's DataAnnotations (and those of the DTOs it carries) before it reaches the handler,
/// no matter who sends it: a controller, n8n or a background job.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        Collect(request, prefix: null, errors);

        // One level deeper: DTOs nested in the command (e.g. UpdateUserCommand.Data)
        foreach (var prop in typeof(TRequest).GetProperties())
        {
            if (prop.PropertyType.Namespace?.StartsWith("Application.DTOs") == true && prop.GetValue(request) is { } dto)
                Collect(dto, prop.Name, errors);
        }

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        return next();
    }

    private static void Collect(object instance, string? prefix, Dictionary<string, string[]> errors)
    {
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true))
            return;

        foreach (var result in results)
        foreach (var member in result.MemberNames.DefaultIfEmpty(string.Empty))
        {
            var key = prefix is null ? member : $"{prefix}.{member}";
            errors[key] = [.. errors.GetValueOrDefault(key, []), result.ErrorMessage ?? "Invalid value."];
        }
    }
}
