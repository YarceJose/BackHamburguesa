using Api.Security;
using Application.DTOs.Auth;
using Application.DTOs.Users;
using Application.Features.Auth;
using Application.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>Inicio de sesión del personal (Admin, Cook, Delivery) desde Front-Hamburguesa.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>Inicia sesión y devuelve un JWT. Los clientes (Customer) no pueden iniciar sesión.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await mediator.Send(new LoginCommand(request), ct));

    /// <summary>Datos del usuario autenticado.</summary>
    [HttpGet("me")]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken ct) =>
        Ok(await mediator.Send(new GetProfileQuery(User.GetUserId()), ct));

    /// <summary>Cambia la contraseña del usuario autenticado.</summary>
    [HttpPut("me/password")]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await mediator.Send(new ChangeOwnPasswordCommand(User.GetUserId(), request), ct);
        return NoContent();
    }
}
