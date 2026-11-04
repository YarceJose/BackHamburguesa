using System.ComponentModel.DataAnnotations;
using Domain.Entities;
using Domain.Enums;

namespace Application.DTOs.Users;

/// <summary>
/// Creates a user. Admin/Cook/Delivery require username and password (email optional).
/// Customer requires a phone and must NOT send a username or password.
/// </summary>
public sealed record CreateUserRequest(
    [Required, StringLength(120, MinimumLength = 2)] string FullName,
    [StringLength(30, MinimumLength = 3)] string? Username,
    [EmailAddress, StringLength(150)] string? Email,
    [Phone, StringLength(20)] string? Phone,
    [StringLength(100, MinimumLength = 8)] string? Password,
    [Required, EnumDataType(typeof(UserRole))] UserRole Role);

public sealed record UpdateUserRequest(
    [Required, StringLength(120, MinimumLength = 2)] string FullName,
    [StringLength(30, MinimumLength = 3)] string? Username,
    [EmailAddress, StringLength(150)] string? Email,
    [Phone, StringLength(20)] string? Phone,
    [Required, EnumDataType(typeof(UserRole))] UserRole Role);

public sealed record ResetPasswordRequest(
    [Required, StringLength(100, MinimumLength = 8)] string NewPassword);

public sealed record UserResponse(
    int Id,
    string FullName,
    string? Username,
    string? Email,
    string? Phone,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastLoginAt)
{
    public static UserResponse From(User u) =>
        new(u.Id, u.FullName, u.Username, u.Email, u.Phone, u.Role, u.IsActive, u.CreatedAt, u.UpdatedAt, u.LastLoginAt);
}
