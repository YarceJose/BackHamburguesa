using System.ComponentModel.DataAnnotations;
using Domain.Entities;
using Domain.Enums;

namespace Application.DTOs.Users;

/// <summary>
/// Crea un usuario. Para Admin/Cook/Delivery el correo y la contraseña son obligatorios.
/// Para Customer el teléfono es obligatorio y NO se envía contraseña.
/// </summary>
public sealed record CreateUserRequest(
    [Required, StringLength(120, MinimumLength = 2)] string FullName,
    [EmailAddress, StringLength(150)] string? Email,
    [Phone, StringLength(20)] string? Phone,
    [StringLength(100, MinimumLength = 8)] string? Password,
    [Required, EnumDataType(typeof(UserRole))] UserRole Role);

public sealed record UpdateUserRequest(
    [Required, StringLength(120, MinimumLength = 2)] string FullName,
    [EmailAddress, StringLength(150)] string? Email,
    [Phone, StringLength(20)] string? Phone,
    [Required, EnumDataType(typeof(UserRole))] UserRole Role);

public sealed record ResetPasswordRequest(
    [Required, StringLength(100, MinimumLength = 8)] string NewPassword);

public sealed record UserResponse(
    int Id,
    string FullName,
    string? Email,
    string? Phone,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastLoginAt)
{
    public static UserResponse From(User u) =>
        new(u.Id, u.FullName, u.Email, u.Phone, u.Role, u.IsActive, u.CreatedAt, u.UpdatedAt, u.LastLoginAt);
}
