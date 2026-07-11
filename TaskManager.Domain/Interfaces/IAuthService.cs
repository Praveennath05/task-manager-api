using TaskManager.Domain.Common;

namespace TaskManager.Domain.Interfaces;

public interface IAuthService
{
    Task<Result<string>> RegisterAsync(string email, string password, CancellationToken cancellationToken);

    // ── CHANGED ─────────────────────────────────────────
    // Now returns AuthResult (both tokens) instead of just a string
    Task<Result<AuthResult>> LoginAsync(string email, string password, CancellationToken cancellationToken);
    // ─────────────────────────────────────────────────

    // ── NEW METHOD ──────────────────────────────────────
    // Takes an old refresh token, validates it, and if valid,
    // issues a brand new AuthResult (new access + new refresh token)
    Task<Result<AuthResult>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
    Task<Result<string>> ConfirmEmailAsync(string userId, string token, CancellationToken cancellationToken);
}