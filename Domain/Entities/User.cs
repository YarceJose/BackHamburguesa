using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

public sealed class User
{
    private User() { }                       // requerido por EF Core

    public int Id { get; private set; }
    public string FullName { get; private set; } = null!;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    /// <summary>Solo el personal activo con contraseña puede iniciar sesión. Los clientes nunca.</summary>
    public bool CanLogin => Role.IsStaff() && IsActive && PasswordHash is not null;

    /// <summary>Crea un usuario del personal (admin, cocinero o domiciliario) con credenciales.</summary>
    public static User CreateStaff(string fullName, string email, string? phone, string passwordHash, UserRole role)
    {
        if (!role.IsStaff())
            throw new DomainException("Solo los roles Admin, Cook y Delivery pueden tener credenciales.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("El correo es obligatorio para el personal.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("La contraseña es obligatoria para el personal.");

        return new User
        {
            FullName = NormalizeName(fullName),
            Email = NormalizeEmail(email),
            Phone = NormalizePhone(phone),
            PasswordHash = passwordHash,
            Role = role
        };
    }

    /// <summary>Crea un cliente. No tiene contraseña: no se registra ni ingresa al sistema.</summary>
    public static User CreateCustomer(string fullName, string phone, string? email)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainException("El teléfono es obligatorio para un cliente.");

        return new User
        {
            FullName = NormalizeName(fullName),
            Email = string.IsNullOrWhiteSpace(email) ? null : NormalizeEmail(email),
            Phone = NormalizePhone(phone),
            PasswordHash = null,
            Role = UserRole.Customer
        };
    }

    public void UpdateProfile(string fullName, string? email, string? phone)
    {
        if (Role.IsStaff() && string.IsNullOrWhiteSpace(email))
            throw new DomainException("El correo es obligatorio para el personal.");
        if (Role == UserRole.Customer && string.IsNullOrWhiteSpace(phone))
            throw new DomainException("El teléfono es obligatorio para un cliente.");

        FullName = NormalizeName(fullName);
        Email = string.IsNullOrWhiteSpace(email) ? null : NormalizeEmail(email);
        Phone = NormalizePhone(phone);
        Touch();
    }

    /// <summary>El rol solo cambia entre roles del personal: un cliente no pasa a tener credenciales.</summary>
    public void ChangeRole(UserRole newRole)
    {
        if (newRole == Role) return;
        if (!Role.IsStaff() || !newRole.IsStaff())
            throw new DomainException("No se puede convertir un cliente en personal ni un miembro del personal en cliente.");

        Role = newRole;
        Touch();
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (!Role.IsStaff())
            throw new DomainException("Los clientes no tienen contraseña.");

        PasswordHash = newPasswordHash;
        Touch();
    }

    public void RegisterLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        DeletedAt = DateTime.UtcNow;
        Touch();
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        DeletedAt = null;
        Touch();
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string NormalizeName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("El nombre es obligatorio.");
        return fullName.Trim();
    }

    private static string? NormalizePhone(string? phone) =>
        string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
