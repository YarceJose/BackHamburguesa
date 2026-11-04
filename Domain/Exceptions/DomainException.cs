namespace Domain.Exceptions;

/// <summary>A business rule enforced by the domain model was violated.</summary>
public class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
