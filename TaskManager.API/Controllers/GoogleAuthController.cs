using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Infrastructure.Services;

namespace TaskManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GoogleAuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly TokenService _tokenService;

    public GoogleAuthController(UserManager<IdentityUser> userManager, TokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    // ── STEP 1 — REDIRECT TO GOOGLE ─────────────────────
    // GET api/googleauth/login
    // The frontend button/link points here — this endpoint
    // kicks off the redirect to Google's login page
    [HttpGet("login")]
    public IActionResult LoginWithGoogle()
    {
        // ── CHALLENGE ──────────────────────────────────
        // Tells ASP.NET Core "start the Google OAuth flow"
        // RedirectUri = where Google sends the user back to
        // AFTER we've processed their login (our own callback below)
        var redirectUrl = Url.Action(nameof(GoogleCallback));
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        // ─────────────────────────────────────────────────
    }

    // ── STEP 2 — HANDLE GOOGLE'S RESPONSE ────────────────
    // GET api/googleauth/callback
    // Google redirects here (via our internal /signin-google
    // middleware first) after the user approves login
    [HttpGet("callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        // ── READ THE TEMPORARY "External" COOKIE ─────────
        // This holds the claims Google gave us — email, name, etc
        var result = await HttpContext.AuthenticateAsync("External");
        if (!result.Succeeded || result.Principal == null)
            return BadRequest("Google authentication failed");
        // ─────────────────────────────────────────────────

        var email = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
            return BadRequest("Could not retrieve email from Google");

        // ── FIND OR CREATE USER ──────────────────────────
        // If someone already registered with this email (password
        // OR previous Google login), reuse that account
        // Otherwise, create a new one — no password needed,
        // since they'll always log in via Google going forward
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new IdentityUser { Email = email, UserName = email, EmailConfirmed = true };
            var createResult = await _userManager.CreateAsync(user);

            if (!createResult.Succeeded)
                return BadRequest("Failed to create user account");

            await _userManager.AddToRoleAsync(user, "User");
        }
        // ─────────────────────────────────────────────────

        // ── ISSUE OUR OWN JWT ─────────────────────────────
        // From this point forward, this user is indistinguishable
        // from a normal password-login user — same JWT shape,
        // same claims, works with all your existing [Authorize] code
        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user, roles);
        // ─────────────────────────────────────────────────

        // ── CLEAN UP THE TEMPORARY COOKIE ─────────────────
        await HttpContext.SignOutAsync("External");

        // ── RETURN THE JWT ────────────────────────────────
        // For now, return as JSON — later, once Blazor is built,
        // this might redirect back to the frontend with the token
        // in the URL or via a different mechanism
        return Ok(new { AccessToken = token });
        // ─────────────────────────────────────────────────
    }
}