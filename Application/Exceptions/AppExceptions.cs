using Domain.Errors;

namespace Application.Exceptions;

/// <summary>Application errors; the API turns them into ProblemDetails with an HTTP status and a "code".</summary>
public abstract class AppException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class NotFoundException(string code, string message) : AppException(code, message);
public sealed class ConflictException(string code, string message) : AppException(code, message);
public sealed class BusinessRuleException(string code, string message) : AppException(code, message);

public sealed class InvalidCredentialsException()
    : AppException(ErrorCodes.InvalidCredentials, "Invalid username or password.");

public sealed class ValidationException(IDictionary<string, string[]> errors)
    : AppException(ErrorCodes.ValidationFailed, "One or more fields are invalid.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class ForbiddenException(string message)
    : AppException(ErrorCodes.Forbidden, message);
