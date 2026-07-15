using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?
            .FindFirstValue(ClaimTypes.NameIdentifier);

    // ── JTI ─────────────────────────────────────────────
    // Remember TokenService.cs — we added this exact claim:
    // new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    public string? Jti =>
        _httpContextAccessor.HttpContext?.User?
            .FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti);
    // ─────────────────────────────────────────────────

    // ── TOKEN EXPIRY ──────────────────────────────────────
    // JWTs carry a standard "exp" claim — Unix timestamp
    // of when the token expires. We read it and convert
    // it back into a proper DateTime
    public DateTime? TokenExpiry
    {
        get
        {
            var expClaim = _httpContextAccessor.HttpContext?.User?
                .FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Exp);

            if (string.IsNullOrEmpty(expClaim) || !long.TryParse(expClaim, out var expSeconds))
                return null;

            return DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
        }
    }
    // ─────────────────────────────────────────────────────
}