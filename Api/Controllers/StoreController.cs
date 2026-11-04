using Api.Security;
using Application.DTOs.Store;
using Application.Features.Catalog;
using Application.Features.Store;
using Application.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>Store status, menu, delivery sectors and where to pay (public), plus the admin's store rules.</summary>
[ApiController]
[Route("api/store")]
[Produces("application/json")]
public class StoreController(IMediator mediator) : ControllerBase
{
    /// <summary>Is the store taking orders, why not, and the current estimated time.</summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<ActionResult<StoreStatusResponse>> Status(CancellationToken ct) =>
        Ok(await mediator.Send(new GetStoreStatusQuery(), ct));

    [HttpGet("menu")]
    [AllowAnonymous]
    public async Task<ActionResult<List<MenuProductResponse>>> Menu(CancellationToken ct) =>
        Ok(await mediator.Send(new GetMenuQuery(), ct));

    /// <summary>Picture uploaded by the admin. The ?v= in the URL changes with every new picture, so it can be cached.</summary>
    [HttpGet("products/{id:int}/image")]
    [AllowAnonymous]
    [ResponseCache(Duration = 86400)]
    public async Task<IActionResult> ProductImage(int id, CancellationToken ct)
    {
        var file = await mediator.Send(new GetProductImageQuery(id), ct);
        return File(file.Content, file.ContentType);
    }

    /// <summary>Active delivery sectors (veredas, barrios). The customer can also write "Otro".</summary>
    [HttpGet("sectors")]
    [AllowAnonymous]
    public async Task<ActionResult<List<SectorResponse>>> Sectors(CancellationToken ct) =>
        Ok(await mediator.Send(new GetSectorsQuery(OnlyActive: true), ct));

    /// <summary>Accounts and QR codes for transfers.</summary>
    [HttpGet("payment-accounts")]
    [AllowAnonymous]
    public async Task<ActionResult<List<PaymentAccountResponse>>> PaymentAccounts(CancellationToken ct) =>
        Ok(await mediator.Send(new GetPaymentAccountsQuery(OnlyActive: true), ct));

    [HttpGet("payment-accounts/{id:int}/qr")]
    [AllowAnonymous]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> PaymentQr(int id, CancellationToken ct)
    {
        var file = await mediator.Send(new GetPaymentQrQuery(id), ct);
        return File(file.Content, file.ContentType);
    }

    // ---------- Admin ----------

    [HttpGet("settings")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<StoreSettingsDto>> Settings(CancellationToken ct) =>
        Ok(await mediator.Send(new GetStoreSettingsQuery(), ct));

    /// <summary>Schedule, open/closed, load-based delay, capacity and alert times.</summary>
    [HttpPut("settings")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<StoreSettingsDto>> UpdateSettings(StoreSettingsDto request, CancellationToken ct) =>
        Ok(await mediator.Send(new UpdateStoreSettingsCommand(request), ct));

    /// <summary>"Cerrar jornada": stop taking orders and sign out kitchen and delivery (admin stays signed in).</summary>
    [HttpPost("close-day")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<StoreStatusResponse>> CloseDay(CancellationToken ct) =>
        Ok(await mediator.Send(new CloseDayCommand(), ct));

    [HttpPost("open-day")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<StoreStatusResponse>> OpenDay(CancellationToken ct) =>
        Ok(await mediator.Send(new OpenDayCommand(), ct));
}
