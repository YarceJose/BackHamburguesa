using Api.Security;
using Application.Abstractions;
using Application.DTOs.Orders;
using Application.Features.Orders;
using Application.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

/// <summary>
/// Orders. Customers (no sign-in) create, follow and pay; staff move them through the kitchen and delivery.
/// </summary>
[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersController(IMediator mediator) : ControllerBase
{
    // ---------- Customer (public) ----------

    /// <summary>Creates an order. Cash goes straight to the kitchen; transfer waits for the receipt.</summary>
    [HttpPost]
    [AllowAnonymous, EnableRateLimiting(RateLimits.Orders)]
    [ProducesResponseType<OrderCreatedResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderCreatedResponse>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(new CreateOrderCommand(request), ct);
        return CreatedAtAction(nameof(Track), new { code = created.Code }, created);
    }

    /// <summary>Order status for the customer, by its public code.</summary>
    [HttpGet("track/{code}")]
    [AllowAnonymous]
    [ProducesResponseType<PublicOrderResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicOrderResponse>> Track(string code, CancellationToken ct) =>
        Ok(await mediator.Send(new TrackOrderQuery(code), ct));

    /// <summary>Uploads the transfer receipt (JPG, PNG, WEBP or PDF, max 5 MB).</summary>
    [HttpPost("track/{code}/receipt")]
    [AllowAnonymous, EnableRateLimiting(RateLimits.Orders)]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<PublicOrderResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicOrderResponse>> UploadReceipt(string code, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return Ok(await mediator.Send(new UploadReceiptCommand(code, new UploadedFile(stream, file.Length)), ct));
    }

    // ---------- Staff ----------

    /// <summary>Open orders (pending payment, kitchen, delivery) and the latest finished ones.</summary>
    [HttpGet("board")]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType<OrderBoardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderBoardResponse>> Board(CancellationToken ct) =>
        Ok(await mediator.Send(new GetOrderBoardQuery(), ct));

    /// <summary>Kitchen starts preparing.</summary>
    [HttpPost("{id:int}/start")]
    [Authorize(Policy = Policies.Kitchen)]
    public Task<ActionResult<OrderResponse>> Start(int id, CancellationToken ct) => Advance(id, OrderAction.Start, ct);

    /// <summary>Kitchen: "Salió de cocina".</summary>
    [HttpPost("{id:int}/left-kitchen")]
    [Authorize(Policy = Policies.Kitchen)]
    public Task<ActionResult<OrderResponse>> LeftKitchen(int id, CancellationToken ct) => Advance(id, OrderAction.LeftKitchen, ct);

    /// <summary>Delivery: "Lo tengo".</summary>
    [HttpPost("{id:int}/picked-up")]
    [Authorize(Policy = Policies.Dispatch)]
    public Task<ActionResult<OrderResponse>> PickedUp(int id, CancellationToken ct) => Advance(id, OrderAction.PickedUp, ct);

    /// <summary>Delivery: "Entregado".</summary>
    [HttpPost("{id:int}/delivered")]
    [Authorize(Policy = Policies.Dispatch)]
    public Task<ActionResult<OrderResponse>> Delivered(int id, CancellationToken ct) => Advance(id, OrderAction.Delivered, ct);

    // ---------- Admin ----------

    /// <summary>The receipt image or PDF the customer uploaded.</summary>
    [HttpGet("{id:int}/receipt")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Receipt(int id, CancellationToken ct)
    {
        var file = await mediator.Send(new GetReceiptQuery(id), ct);
        return File(file.Content, file.ContentType);
    }

    /// <summary>Payment checked against the bank: the order goes to the kitchen.</summary>
    [HttpPost("{id:int}/payment/approve")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<OrderResponse>> ApprovePayment(int id, CancellationToken ct) =>
        Ok(await mediator.Send(new ApprovePaymentCommand(id, User.GetUserId()), ct));

    /// <summary>Receipt not valid: the customer sees the reason and can upload another one.</summary>
    [HttpPost("{id:int}/payment/reject")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<OrderResponse>> RejectPayment(int id, RejectPaymentRequest request, CancellationToken ct) =>
        Ok(await mediator.Send(new RejectPaymentCommand(id, User.GetUserId(), request.Reason), ct));

    [HttpPost("{id:int}/cancel")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<OrderResponse>> Cancel(int id, CancelOrderRequest request, CancellationToken ct) =>
        Ok(await mediator.Send(new CancelOrderCommand(id, request.Reason), ct));

    private async Task<ActionResult<OrderResponse>> Advance(int id, OrderAction action, CancellationToken ct) =>
        Ok(await mediator.Send(new AdvanceOrderCommand(id, action, User.GetUserId(), User.GetRole()), ct));
}
