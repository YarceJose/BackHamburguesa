using Api.Security;
using Application.DTOs.Users;
using Application.Features.Customers;
using Application.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Endpoints for n8n workflows (WhatsApp, forms, etc.).
/// Authenticated with the X-Api-Key header, so customers get registered
/// without ever going through the Front-Hamburguesa login.
/// </summary>
[ApiController]
[Route("api/integrations/n8n/customers")]
[Authorize(Policy = Policies.N8n)]
[Produces("application/json")]
public class N8nCustomersController(IMediator mediator) : ControllerBase
{
    /// <summary>Finds a customer by phone.</summary>
    [HttpGet("{phone}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetByPhone(string phone, CancellationToken ct) =>
        Ok(await mediator.Send(new GetCustomerByPhoneQuery(phone), ct));

    /// <summary>Creates the customer if missing (by phone) or updates their data.</summary>
    [HttpPost]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UserResponse>> Upsert(UpsertCustomerRequest request, CancellationToken ct)
    {
        var (customer, created) = await mediator.Send(new UpsertCustomerCommand(request), ct);
        return created
            ? CreatedAtAction(nameof(GetByPhone), new { phone = customer.Phone }, customer)
            : Ok(customer);
    }
}
