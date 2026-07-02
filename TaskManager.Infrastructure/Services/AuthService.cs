using Microsoft.AspNetCore.Identity;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly TokenService _tokenService;

    // ── NEW DEPENDENCY ─────────────────────────────────────
    // Needed to save, find, and revoke refresh tokens in the database
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    // ─────────────────────────────────────────────────────

    public AuthService(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        TokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<Result<string>> RegisterAsync(string email, string password, CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
            return Result<string>.Failure("Email already registered");

        var user = new IdentityUser
        {
            Email = email,
            UserName = email
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return Result<string>.Failure(
                string.Join(", ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, "User");

        return Result<string>.Success("Registration successful");
    }

    public async Task<Result<AuthResult>> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return Result<AuthResult>.Failure("Invalid email or password");

        var result = await _signInManager.CheckPasswordSignInAsync(
            user, password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Result<AuthResult>.Failure("Invalid email or password");

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateToken(user, roles);

        // ── ISSUE REFRESH TOKEN ─────────────────────────────
        // A separate, long-lived token — saved to the database
        // so we can verify and revoke it later
        var refreshToken = await GenerateAndSaveRefreshTokenAsync(user.Id, cancellationToken);
        // ─────────────────────────────────────────────────

        return Result<AuthResult>.Success(new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        });
    }

    public async Task<Result<AuthResult>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        // ── FIND THE TOKEN ──────────────────────────────────
        var existingToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken);

        // ── VALIDATE ─────────────────────────────────────────
        // Reject if token doesn't exist, is revoked, or expired
        // IsActive is the computed property we built on RefreshToken entity
        if (existingToken == null || !existingToken.IsActive)
            return Result<AuthResult>.Failure("Invalid or expired refresh token");
        // ─────────────────────────────────────────────────

        var user = await _userManager.FindByIdAsync(existingToken.UserId);
        if (user == null)
            return Result<AuthResult>.Failure("User not found");

        // ── TOKEN ROTATION ──────────────────────────────────
        // Best practice: revoke the OLD refresh token and issue a NEW one
        // instead of reusing it. If a stolen token gets used once,
        // rotation limits how long an attacker can keep using it
        await _refreshTokenRepository.RevokeAsync(existingToken, cancellationToken);
        // ─────────────────────────────────────────────────

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _tokenService.GenerateToken(user, roles);
        var newRefreshToken = await GenerateAndSaveRefreshTokenAsync(user.Id, cancellationToken);

        return Result<AuthResult>.Success(new AuthResult
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        });
    }

    // ── PRIVATE HELPER ─────────────────────────────────────
    // Shared by LoginAsync and RefreshTokenAsync — avoids duplicating
    // the token creation logic in two places
    private async Task<string> GenerateAndSaveRefreshTokenAsync(string userId, CancellationToken cancellationToken)
    {
        // ── RANDOM TOKEN ─────────────────────────────────
        // Not a JWT — just a cryptographically random string
        // 64 random bytes, converted to a URL-safe Base64 string
        var randomBytes = new byte[64];
        System.Security.Cryptography.RandomNumberGenerator.Fill(randomBytes);
        var tokenValue = Convert.ToBase64String(randomBytes);
        // ─────────────────────────────────────────────────

        var refreshToken = new RefreshToken
        {
            Token = tokenValue,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // long-lived — 7 days
            IsRevoked = false
        };

        await _refreshTokenRepository.CreateAsync(refreshToken, cancellationToken);

        return tokenValue;
    }
    // ─────────────────────────────────────────────────────
}