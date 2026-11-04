using System.ComponentModel.DataAnnotations;
using Application.DTOs.Users;

namespace Application.DTOs.Auth;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record LoginResponse(string Token, DateTime ExpiresAt, UserResponse User);

public sealed record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, StringLength(100, MinimumLength = 8)] string NewPassword);
