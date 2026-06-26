using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Features.Auth.Commands;

namespace TaskManager.API.Controllers;

// ── CONTROLLER ────────────────────────────────────────
// Entry point for all auth-related HTTP requests
// Only talks to MediatR — never touches Identity directly
// Clean Architecture maintained 
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // ── DEPENDENCY INJECTION ──────────────────────────
    // MediatR is injected — routes commands to handlers
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }
    // ─────────────────────────────────────────────────

    // ── REGISTER ENDPOINT ─────────────────────────────
    // POST api/auth/register
    // Accepts email and password — creates a new user
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(result.ErrorMessage);
    }
    // ─────────────────────────────────────────────────

    // ── LOGIN ENDPOINT ────────────────────────────────
    // POST api/auth/login
    // Accepts email and password — returns JWT token on success
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess
            ? Ok(new { Token = result.Data })
            : Unauthorized(result.ErrorMessage);
    }
    // ─────────────────────────────────────────────────
}