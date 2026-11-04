namespace Domain.Exceptions;

/// <summary>Regla de negocio violada dentro del dominio.</summary>
public class DomainException(string message) : Exception(message);
