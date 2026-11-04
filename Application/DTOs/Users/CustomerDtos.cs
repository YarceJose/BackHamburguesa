using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Users;

/// <summary>Cliente que llega desde n8n (WhatsApp, formularios, etc.). Se identifica por teléfono.</summary>
public sealed record UpsertCustomerRequest(
    [Required, StringLength(120, MinimumLength = 2)] string FullName,
    [Required, Phone, StringLength(20)] string Phone,
    [EmailAddress, StringLength(150)] string? Email);
