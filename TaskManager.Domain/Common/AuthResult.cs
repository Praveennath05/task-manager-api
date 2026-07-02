namespace TaskManager.Domain.Common;

// ── AUTH RESULT ─────────────────────────────────────────
// Carries BOTH tokens together after a successful login
// AccessToken  — short-lived, used for every API call
// RefreshToken — long-lived, used only to get a new AccessToken
public class AuthResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
// ─────────────────────────────────────────────────────