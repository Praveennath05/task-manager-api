using TaskManager.Domain.Common;

namespace TaskManager.Domain.Entities;

// ── REFRESH TOKEN ──────────────────────────────────────
// Represents ONE issued refresh token, stored in the database
// so the server can verify it later and revoke it if needed
// (a plain JWT can't be revoked — this table gives us that control)
public class RefreshToken : BaseEntity
{
    // The actual token string sent to the client
    // A long random string — not a JWT, just a secure random value
    public string Token { get; set; } = string.Empty;

    // Which user this token belongs to
    // IdentityUser's Id is a string (GUID), so we match that type
    public string UserId { get; set; } = string.Empty;

    // When this refresh token stops being valid
    // e.g. 7 days from creation
    public DateTime ExpiresAt { get; set; }

    // ── REVOCATION ─────────────────────────────────────
    // If true, this token can no longer be used —
    // even if ExpiresAt hasn't passed yet
    // Set to true on logout, or if we suspect it was stolen
    public bool IsRevoked { get; set; } = false;
    // ─────────────────────────────────────────────────

    // ── HELPER PROPERTY ────────────────────────────────
    // Computed, not stored in DB — quick way to check validity
    // "Is this token still usable right now?"
    public bool IsActive => !IsRevoked && ExpiresAt > DateTime.UtcNow;
    // ─────────────────────────────────────────────────
}