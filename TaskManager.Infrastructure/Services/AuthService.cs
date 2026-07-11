using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly TokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    // ── NEW DEPENDENCY ─────────────────────────────────────
    // Needed to actually send the confirmation email
    private readonly IEmailService _emailService;
    // ─────────────────────────────────────────────────────

    public AuthService(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        TokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _emailService = emailService;
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
            // ── NOTE ──────────────────────────────────────
            // EmailConfirmed defaults to false automatically —
            // we don't need to set it explicitly here
            // ─────────────────────────────────────────────
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return Result<string>.Failure(
                string.Join(", ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, "User");

        // ── GENERATE CONFIRMATION TOKEN ───────────────────────
        // Identity's built-in mechanism — a secure, single-use,
        // time-limited token tied specifically to this user
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        // ── URL-ENCODE THE TOKEN ──────────────────────────────
        // Confirmation tokens contain special characters (+, /, =)
        // that break URLs unless properly encoded — WebEncoders
        // handles this safely
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        // ─────────────────────────────────────────────────────

        // ── BUILD THE CONFIRMATION LINK ───────────────────────
        // Points to our own API endpoint that will verify the token
        // NOTE: hardcoded localhost URL for now — in production this
        // would come from configuration (the real deployed API URL)
        var confirmationLink =
            $"http://localhost:5097/api/auth/confirm-email?userId={user.Id}&token={encodedToken}";
        // ─────────────────────────────────────────────────────

        var subject = "Confirm your Task Manager account";
        var htmlBody = $@"
            <h2>Welcome to Task Manager</h2>
            <p>Please confirm your email address by clicking the link below:</p>
            <p><a href='{confirmationLink}'>Confirm Email</a></p>
            <p>If you didn't create this account, you can safely ignore this email.</p>";

        await _emailService.SendEmailAsync(email, subject, htmlBody, cancellationToken);

        return Result<string>.Success("Registration successful. Please check your email to confirm your account.");
    }

    public async Task<Result<AuthResult>> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return Result<AuthResult>.Failure("Invalid email or password");

        // ── EMAIL CONFIRMATION CHECK ──────────────────────────
        // Block login entirely until the user has clicked their
        // confirmation link — this is the actual enforcement point
        if (!user.EmailConfirmed)
            return Result<AuthResult>.Failure("Please confirm your email before logging in");
        // ─────────────────────────────────────────────────────

        var result = await _signInManager.CheckPasswordSignInAsync(
            user, password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Result<AuthResult>.Failure("Invalid email or password");

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateToken(user, roles);
        var refreshToken = await GenerateAndSaveRefreshTokenAsync(user.Id, cancellationToken);

        return Result<AuthResult>.Success(new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        });
    }

    public async Task<Result<AuthResult>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var existingToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken);

        if (existingToken == null || !existingToken.IsActive)
            return Result<AuthResult>.Failure("Invalid or expired refresh token");

        var user = await _userManager.FindByIdAsync(existingToken.UserId);
        if (user == null)
            return Result<AuthResult>.Failure("User not found");

        await _refreshTokenRepository.RevokeAsync(existingToken, cancellationToken);

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _tokenService.GenerateToken(user, roles);
        var newRefreshToken = await GenerateAndSaveRefreshTokenAsync(user.Id, cancellationToken);

        return Result<AuthResult>.Success(new AuthResult
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        });
    }

    // ── NEW METHOD — CONFIRM EMAIL ────────────────────────────
    // Called by the controller when the user clicks the link
    public async Task<Result<string>> ConfirmEmailAsync(string userId, string token, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result<string>.Failure("Invalid confirmation link");

        // ── DECODE THE TOKEN ───────────────────────────────
        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch
        {
            return Result<string>.Failure("Invalid confirmation link");
        }
        // ─────────────────────────────────────────────────

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
            return Result<string>.Failure("Invalid or expired confirmation link");

        return Result<string>.Success("Email confirmed successfully. You can now log in.");
    }
    // ─────────────────────────────────────────────────────

    private async Task<string> GenerateAndSaveRefreshTokenAsync(string userId, CancellationToken cancellationToken)
    {
        var randomBytes = new byte[64];
        System.Security.Cryptography.RandomNumberGenerator.Fill(randomBytes);
        var tokenValue = Convert.ToBase64String(randomBytes);

        var refreshToken = new RefreshToken
        {
            Token = tokenValue,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        await _refreshTokenRepository.CreateAsync(refreshToken, cancellationToken);

        return tokenValue;
    }
}