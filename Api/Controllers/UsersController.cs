using Api.Security;
using Application.DTOs.Users;
using Application.Features.Users;
using Application.Mediator;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>User management. Only an Admin creates, edits and deactivates users (no self sign-up).</summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = Policies.AdminOnly)]
[Produces("application/json")]
public class UsersController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists users, optionally filtered by role.</summary>
    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetAll(
        [FromQuery] UserRole? role, [FromQuery] bool includeInactive = false, CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetUsersQuery(role, includeInactive), ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(int id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetUserByIdQuery(id), ct));

    /// <summary>
    /// Creates a user. Admin/Cook/Delivery require email and password.
    /// Customer requires a phone and has no password.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(new CreateUserCommand(request), ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResponse>> Update(int id, UpdateUserRequest request, CancellationToken ct) =>
        Ok(await mediator.Send(new UpdateUserCommand(id, User.GetUserId(), request), ct));

    /// <summary>An Admin sets a new password for a staff member.</summary>
    [HttpPut("{id:int}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request, CancellationToken ct)
    {
        await mediator.Send(new ResetPasswordCommand(id, request), ct);
        return NoContent();
    }

    /// <summary>Deactivates the user (soft delete): they can no longer sign in.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await mediator.Send(new DeactivateUserCommand(id, User.GetUserId()), ct);
        return NoContent();
    }

    [HttpPost("{id:int}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Activate(int id, CancellationToken ct)
    {
        await mediator.Send(new ActivateUserCommand(id), ct);
        return NoContent();
    }
}
