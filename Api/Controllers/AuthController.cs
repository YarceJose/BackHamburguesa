using Api.Security;
using Application.DTOs.Auth;
using Application.DTOs.Users;
using Application.Features.Auth;
using Application.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

/// <summary>Staff sign-in (admin, cook, delivery) with username and password. Customers never sign in.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>Returns a JWT. Kitchen and delivery tokens stop working when the admin closes the day.</summary>
    [HttpPost("login")]
    [AllowAnonymous, EnableRateLimiting(RateLimits.Auth)]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await mediator.Send(new LoginCommand(request), ct));

    /// <summary>Current user's profile.</summary>
    [HttpGet("me")]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken ct) =>
        Ok(await mediator.Send(new GetProfileQuery(User.GetUserId()), ct));

    /// <summary>Changes the current user's password.</summary>
    [HttpPut("me/password")]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await mediator.Send(new ChangeOwnPasswordCommand(User.GetUserId(), request), ct);
        return NoContent();
    }
}
