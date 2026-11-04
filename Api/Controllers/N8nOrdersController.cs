using Api.Security;
using Application.Abstractions;
using Application.DTOs.Orders;
using Application.Features.Orders;
using Application.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Orders taken by n8n (e.g. a WhatsApp conversation). Same rules as the app: store hours, capacity,
/// cash or transfer + receipt. Authenticated with the X-Api-Key header.
/// </summary>
[ApiController]
[Route("api/integrations/n8n/orders")]
[Authorize(Policy = Policies.N8n)]
[Produces("application/json")]
public class N8nOrdersController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderCreatedResponse>> Create(CreateOrderRequest request, CancellationToken ct) =>
        Ok(await mediator.Send(new CreateOrderCommand(request), ct));

    [HttpGet("{code}")]
    public async Task<ActionResult<PublicOrderResponse>> Track(string code, CancellationToken ct) =>
        Ok(await mediator.Send(new TrackOrderQuery(code), ct));

    /// <summary>Receipt image the customer sent by WhatsApp.</summary>
    [HttpPost("{code}/receipt")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PublicOrderResponse>> UploadReceipt(string code, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return Ok(await mediator.Send(new UploadReceiptCommand(code, new UploadedFile(stream, file.Length)), ct));
    }
}
