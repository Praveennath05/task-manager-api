using MediatR;
using System.Security.Claims;
using TaskManager.Domain.Common;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Features.Auth.Commands;

// ── COMMAND ───────────────────────────────────────────
// Carries the current access token's claims so we know
// which specific token to blacklist
public record LogoutCommand(string Jti, DateTime TokenExpiry) : IRequest<Result<string>>;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result<string>>
{
    private readonly ICacheService _cache;

    public LogoutCommandHandler(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<Result<string>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // ── CALCULATE REMAINING TIME ──────────────────────
        // Only need to blacklist until the token would have
        // naturally expired anyway — no point storing forever
        var remainingTime = request.TokenExpiry - DateTime.UtcNow;
        if (remainingTime <= TimeSpan.Zero)
            return Result<string>.Success("Already expired"); // nothing to blacklist
        // ─────────────────────────────────────────────────

        // ── ADD TO BLACKLIST ───────────────────────────────
        // Key format: "blacklist:{jti}" — value doesn't matter,
        // just its EXISTENCE in Redis is the signal
        await _cache.SetAsync($"blacklist:{request.Jti}", true, remainingTime, cancellationToken);
        // ─────────────────────────────────────────────────

        return Result<string>.Success("Logged out successfully");
    }
}