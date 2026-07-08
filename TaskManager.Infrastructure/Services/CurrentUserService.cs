using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    // ── DEPENDENCY INJECTION ──────────────────────────────
    // IHttpContextAccessor gives access to the current HTTP request,
    // including the authenticated user's claims (set by JWT middleware)
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    // ─────────────────────────────────────────────────────

    public string? UserId =>
        // ── READ FROM JWT CLAIMS ────────────────────────
        // Remember TokenService.cs — we added this exact claim
        // when generating the JWT: new Claim(ClaimTypes.NameIdentifier, user.Id)
        // This reads that same claim back out on every request
        _httpContextAccessor.HttpContext?.User?
            .FindFirstValue(ClaimTypes.NameIdentifier);
        // ─────────────────────────────────────────────────
}