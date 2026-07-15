using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Features.Auth.Commands;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace TaskManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(result.ErrorMessage);
    }

    // ── LOGIN ENDPOINT ────────────────────────────────
    // POST api/auth/login
    // Now returns BOTH tokens — result.Data is an AuthResult
    [HttpPost("login")]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess
            ? Ok(result.Data)  // AuthResult already has AccessToken + RefreshToken as properties
            : Unauthorized(result.ErrorMessage);
    }
    // ─────────────────────────────────────────────────

    // ── REFRESH ENDPOINT ──────────────────────────────
    // POST api/auth/refresh
    // Client sends their (still valid) refresh token here
    // Gets back a brand new access token + refresh token
    // No email/password needed — this is the whole point
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess
            ? Ok(result.Data)
            : Unauthorized(result.ErrorMessage);
    }
    // ─────────────────────────────────────────────────
    // ── CONFIRM EMAIL ENDPOINT ─────────────────────────
    // GET api/auth/confirm-email?userId=...&token=...
    // This is the exact URL the confirmation email links to
    // GET (not POST) because the user reaches it by clicking
    // a link in their email, not submitting a form
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        var command = new ConfirmEmailCommand(userId, token);
        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(result.ErrorMessage);
    }
    // ─────────────────────────────────────────────────
// ── LOGOUT ENDPOINT ────────────────────────────────
    // POST api/auth/logout
    // Requires a valid JWT (the one being logged out) —
    // blacklists it so it can't be used again even though
    // it hasn't technically expired yet
    [HttpPost("logout")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Logout()
    {
        var jti = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti);
        var expClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Exp);

        if (string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(expClaim) || !long.TryParse(expClaim, out var expSeconds))
            return BadRequest("Unable to process logout");

        var tokenExpiry = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;

        var command = new LogoutCommand(jti, tokenExpiry);
        var result = await _mediator.Send(command);

        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.ErrorMessage);
    }
    // ─────────────────────────────────────────────────


}


