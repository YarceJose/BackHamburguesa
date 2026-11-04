using System.ComponentModel.DataAnnotations;
using Application.DTOs.Users;

namespace Application.DTOs.Auth;

/// <summary>Every staff member (admin, cook, delivery) signs in with username and password.</summary>
public sealed record LoginRequest(
    [Required, StringLength(30, MinimumLength = 3)] string Username,
    [Required] string Password);

public sealed record LoginResponse(string Token, DateTime ExpiresAt, UserResponse User);

public sealed record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, StringLength(100, MinimumLength = 8)] string NewPassword);
