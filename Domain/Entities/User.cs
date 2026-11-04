using Domain.Enums;
using Domain.Errors;
using System.Text.RegularExpressions;
using Domain.Exceptions;

namespace Domain.Entities;

public sealed partial class User
{
    private User() { }                       // required by EF Core

    public int Id { get; private set; }
    public string FullName { get; private set; } = null!;
    /// <summary>Sign-in name for staff (admin, cook, delivery). Customers have none.</summary>
    public string? Username { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// Random value copied into every JWT. It changes when the user is deactivated, gets a new password
    /// or a new role, which invalidates all their open sessions at once.
    /// </summary>
    public string SecurityStamp { get; private set; } = NewStamp();

    /// <summary>Only active staff with a password can sign in. Customers never can.</summary>
    public bool CanLogin => Role.IsStaff() && IsActive && PasswordHash is not null;

    /// <summary>Creates a staff user (admin, cook or delivery) who signs in with username and password.</summary>
    public static User CreateStaff(string fullName, string username, string? email, string? phone, string passwordHash, UserRole role)
    {
        if (!role.IsStaff())
            throw new DomainException(ErrorCodes.UserRoleNotStaff, "Only Admin, Cook and Delivery roles can have credentials.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException(ErrorCodes.UserPasswordRequired, "Password is required for staff.");

        return new User
        {
            FullName = NormalizeName(fullName),
            Username = NormalizeUsername(username),
            Email = string.IsNullOrWhiteSpace(email) ? null : NormalizeEmail(email),
            Phone = NormalizePhone(phone),
            PasswordHash = passwordHash,
            Role = role
        };
    }

    /// <summary>Creates a customer. Customers have no password: they never register or sign in.</summary>
    public static User CreateCustomer(string fullName, string phone, string? email)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainException(ErrorCodes.CustomerPhoneRequired, "Phone is required for a customer.");

        return new User
        {
            FullName = NormalizeName(fullName),
            Email = string.IsNullOrWhiteSpace(email) ? null : NormalizeEmail(email),
            Phone = NormalizePhone(phone),
            PasswordHash = null,
            Role = UserRole.Customer
        };
    }

    public void UpdateProfile(string fullName, string? username, string? email, string? phone)
    {
        if (Role == UserRole.Customer && string.IsNullOrWhiteSpace(phone))
            throw new DomainException(ErrorCodes.CustomerPhoneRequired, "Phone is required for a customer.");

        FullName = NormalizeName(fullName);
        if (Role.IsStaff()) Username = NormalizeUsername(username);
        Email = string.IsNullOrWhiteSpace(email) ? null : NormalizeEmail(email);
        Phone = NormalizePhone(phone);
        Touch();
    }

    /// <summary>Roles only change among staff roles: a customer never gains credentials.</summary>
    public void ChangeRole(UserRole newRole)
    {
        if (newRole == Role) return;
        if (!Role.IsStaff() || !newRole.IsStaff())
            throw new DomainException(ErrorCodes.UserRoleChangeNotAllowed, "A customer cannot become staff and staff cannot become a customer.");

        Role = newRole;
        RotateSecurityStamp();
        Touch();
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (!Role.IsStaff())
            throw new DomainException(ErrorCodes.CustomerPasswordNotAllowed, "Customers do not have a password.");

        PasswordHash = newPasswordHash;
        RotateSecurityStamp();
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
        RotateSecurityStamp();
        Touch();
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        DeletedAt = null;
        Touch();
    }

    /// <summary>Signs the user out everywhere (their current tokens stop working).</summary>
    public void RotateSecurityStamp() => SecurityStamp = NewStamp();

    private static string NewStamp() => Guid.NewGuid().ToString("N");

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>3–30 characters: lowercase letters, digits, dot, dash or underscore.</summary>
    public static string NormalizeUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException(ErrorCodes.UserUsernameRequired, "Username is required for staff.");
        var value = username.Trim().ToLowerInvariant();
        if (!UsernamePattern().IsMatch(value))
            throw new DomainException(ErrorCodes.UserUsernameInvalid, "Username must be 3-30 characters: letters, digits, dot, dash or underscore.");
        return value;
    }

    [GeneratedRegex("^[a-z0-9._-]{3,30}$")]
    private static partial Regex UsernamePattern();

    private static string NormalizeName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException(ErrorCodes.UserNameRequired, "Full name is required.");
        return fullName.Trim();
    }

    private static string? NormalizePhone(string? phone) =>
        string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
