using TaskManager.Domain.Entities;

namespace TaskManager.Domain.Interfaces;

// ── INTERFACE ─────────────────────────────────────────
// Same repository pattern as IWorkTaskRepository
// Application layer only knows this contract — never EF Core directly
public interface IRefreshTokenRepository
{
    // Save a newly issued refresh token to the database
    Task CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken);

    // Find a refresh token by its string value — used during the refresh flow
    // to check "does this token exist and is it valid?"
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken);

    // Mark a token as revoked (used on refresh — old token dies,
    // new token is issued) or on logout
    Task RevokeAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
}
// ─────────────────────────────────────────────────────