using System.ComponentModel.DataAnnotations;
using Application.Mediator;
using AppValidationException = Application.Exceptions.ValidationException;

namespace Application.Behaviors;

/// <summary>
/// Valida las DataAnnotations de la petición (y de los DTOs que contenga) antes de llegar al handler,
/// sin importar quién la envíe: un controlador, n8n o un proceso en segundo plano.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        Collect(request, prefix: null, errors);

        // Un nivel más: DTOs anidados dentro del comando (p. ej. UpdateUserCommand.Data)
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
            errors[key] = [.. errors.GetValueOrDefault(key, []), result.ErrorMessage ?? "Valor inválido."];
        }
    }
}
