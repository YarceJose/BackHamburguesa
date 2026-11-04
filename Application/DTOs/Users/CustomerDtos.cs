using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Users;

/// <summary>Customer coming from n8n (WhatsApp, forms, etc.). Identified by phone.</summary>
public sealed record UpsertCustomerRequest(
    [Required, StringLength(120, MinimumLength = 2)] string FullName,
    [Required, Phone, StringLength(20)] string Phone,
    [EmailAddress, StringLength(150)] string? Email);
