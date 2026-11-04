namespace Application.Exceptions;

/// <summary>Errores de aplicación que la API traduce a ProblemDetails con su código HTTP.</summary>
public abstract class AppException(string message) : Exception(message);

public sealed class NotFoundException(string message) : AppException(message);
public sealed class ConflictException(string message) : AppException(message);
public sealed class BusinessRuleException(string message) : AppException(message);
public sealed class InvalidCredentialsException() : AppException("Correo o contraseña incorrectos.");

public sealed class ValidationException(IDictionary<string, string[]> errors)
    : AppException("Uno o más campos no son válidos.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}
