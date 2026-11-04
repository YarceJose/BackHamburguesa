using Api.Security;
using Application.DTOs.Users;
using Application.Features.Customers;
using Application.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Endpoints para los flujos de n8n (WhatsApp, formularios, etc.).
/// Autenticación con la cabecera X-Api-Key. Así los clientes quedan registrados
/// sin pasar nunca por el login de Front-Hamburguesa.
/// </summary>
[ApiController]
[Route("api/integrations/n8n/customers")]
[Authorize(Policy = Policies.N8n)]
[Produces("application/json")]
public class N8nCustomersController(IMediator mediator) : ControllerBase
{
    /// <summary>Busca un cliente por teléfono.</summary>
    [HttpGet("{phone}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetByPhone(string phone, CancellationToken ct) =>
        Ok(await mediator.Send(new GetCustomerByPhoneQuery(phone), ct));

    /// <summary>Crea el cliente si no existe (por teléfono) o actualiza sus datos.</summary>
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
