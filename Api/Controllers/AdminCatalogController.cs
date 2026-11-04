using Api.Security;
using Application.Abstractions;
using Application.DTOs.Store;
using Application.Features.Catalog;
using Application.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>Admin: delivery sectors and payment accounts (QR).</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = Policies.AdminOnly)]
[Produces("application/json")]
public class AdminCatalogController(IMediator mediator) : ControllerBase
{
    // ---------- Sectors ----------
    [HttpGet("sectors")]
    public async Task<ActionResult<List<SectorResponse>>> Sectors(CancellationToken ct) =>
        Ok(await mediator.Send(new GetSectorsQuery(OnlyActive: false), ct));

    [HttpPost("sectors")]
    public async Task<ActionResult<SectorResponse>> CreateSector(SectorRequest request, CancellationToken ct) =>
        Ok(await mediator.Send(new CreateSectorCommand(request), ct));

    [HttpPut("sectors/{id:int}")]
    public async Task<ActionResult<SectorResponse>> UpdateSector(int id, SectorRequest request, CancellationToken ct) =>
        Ok(await mediator.Send(new UpdateSectorCommand(id, request), ct));

    // ---------- Payment accounts ----------
    [HttpGet("payment-accounts")]
    public async Task<ActionResult<List<PaymentAccountResponse>>> PaymentAccounts(CancellationToken ct) =>
        Ok(await mediator.Send(new GetPaymentAccountsQuery(OnlyActive: false), ct));

    [HttpPost("payment-accounts")]
    public async Task<ActionResult<PaymentAccountResponse>> CreatePaymentAccount(PaymentAccountRequest request, CancellationToken ct) =>
        Ok(await mediator.Send(new CreatePaymentAccountCommand(request), ct));

    [HttpPut("payment-accounts/{id:int}")]
    public async Task<ActionResult<PaymentAccountResponse>> UpdatePaymentAccount(int id, PaymentAccountRequest request, CancellationToken ct) =>
        Ok(await mediator.Send(new UpdatePaymentAccountCommand(id, request), ct));

    /// <summary>The QR image the bank app generates for receiving payments (JPG, PNG or WEBP, max 5 MB).</summary>
    [HttpPost("payment-accounts/{id:int}/qr")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PaymentAccountResponse>> UploadQr(int id, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return Ok(await mediator.Send(new UploadPaymentQrCommand(id, new UploadedFile(stream, file.Length)), ct));
    }
}
