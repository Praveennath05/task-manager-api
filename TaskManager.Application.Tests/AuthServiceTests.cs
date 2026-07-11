using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;
using TaskManager.Infrastructure.Services;
using Xunit;
using Microsoft.Extensions.Configuration;

namespace TaskManager.Application.Tests;

public class AuthServiceTests
{
    private static Mock<UserManager<IdentityUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<SignInManager<IdentityUser>> CreateMockSignInManager(
        Mock<UserManager<IdentityUser>> mockUserManager)
    {
        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();

        return new Mock<SignInManager<IdentityUser>>(
            mockUserManager.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            null!, null!, null!, null!);
    }

    private static TokenService CreateTestTokenService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "JwtSettings:SecretKey", "ThisIsAVerySecretKeyForTestingOnly12345678" },
                { "JwtSettings:Issuer", "TestIssuer" },
                { "JwtSettings:Audience", "TestAudience" },
                { "JwtSettings:ExpiryMinutes", "60" }
            })
            .Build();

        return new TokenService(configuration);
    }

    private static Mock<IEmailService> CreateMockEmailService()
    {
        var mock = new Mock<IEmailService>();
        mock.Setup(e => e.SendEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_ShouldCreateUserAndReturnSuccess()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockUserManager = CreateMockUserManager();
        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        var mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        var mockEmailService = CreateMockEmailService();
        var tokenService = CreateTestTokenService();

        mockUserManager
            .Setup(um => um.FindByEmailAsync("new@gmail.com"))
            .ReturnsAsync((IdentityUser?)null);

        mockUserManager
            .Setup(um => um.CreateAsync(It.IsAny<IdentityUser>(), "Password123"))
            .ReturnsAsync(IdentityResult.Success);

        mockUserManager
            .Setup(um => um.AddToRoleAsync(It.IsAny<IdentityUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        // ── NEW — CONFIRMATION TOKEN GENERATION ──────────
        // RegisterAsync now calls this — must be mocked or it throws
        mockUserManager
            .Setup(um => um.GenerateEmailConfirmationTokenAsync(It.IsAny<IdentityUser>()))
            .ReturnsAsync("fake-confirmation-token");
        // ─────────────────────────────────────────────────

        var authService = new AuthService(
            mockUserManager.Object,
            mockSignInManager.Object,
            tokenService,
            mockRefreshTokenRepo.Object,
            mockEmailService.Object);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await authService.RegisterAsync("new@gmail.com", "Password123", CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.Contains("check your email", result.Data);
        // ─────────────────────────────────────────────────

        mockUserManager.Verify(
            um => um.CreateAsync(It.IsAny<IdentityUser>(), "Password123"),
            Times.Once);

        mockUserManager.Verify(
            um => um.AddToRoleAsync(It.IsAny<IdentityUser>(), "User"),
            Times.Once);

        // ── VERIFY — CONFIRMATION EMAIL WAS ACTUALLY SENT ──
        mockEmailService.Verify(
            e => e.SendEmailAsync("new@gmail.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        // ─────────────────────────────────────────────────
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ShouldReturnFailure_AndNeverCallCreate()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockUserManager = CreateMockUserManager();
        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        var mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        var mockEmailService = CreateMockEmailService();
        var tokenService = CreateTestTokenService();

        var existingUser = new IdentityUser { Email = "existing@gmail.com" };

        mockUserManager
            .Setup(um => um.FindByEmailAsync("existing@gmail.com"))
            .ReturnsAsync(existingUser);

        var authService = new AuthService(
            mockUserManager.Object,
            mockSignInManager.Object,
            tokenService,
            mockRefreshTokenRepo.Object,
            mockEmailService.Object);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await authService.RegisterAsync("existing@gmail.com", "Password123", CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Email already registered", result.ErrorMessage);
        // ─────────────────────────────────────────────────

        mockUserManager.Verify(
            um => um.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()),
            Times.Never);
    }

    // ══════════════════════════════════════════════════════
    // LoginAsync TESTS
    // ══════════════════════════════════════════════════════

    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldReturnAuthResultWithTokens()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockUserManager = CreateMockUserManager();
        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        var mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        var mockEmailService = CreateMockEmailService();
        var tokenService = CreateTestTokenService();

        // ── EmailConfirmed = true ────────────────────────
        // Required now — LoginAsync blocks unconfirmed accounts
        var existingUser = new IdentityUser { Id = "user-123", Email = "test@gmail.com", EmailConfirmed = true };
        // ─────────────────────────────────────────────────

        mockUserManager
            .Setup(um => um.FindByEmailAsync("test@gmail.com"))
            .ReturnsAsync(existingUser);

        mockSignInManager
            .Setup(sm => sm.CheckPasswordSignInAsync(existingUser, "CorrectPassword", true))
            .ReturnsAsync(SignInResult.Success);

        mockUserManager
            .Setup(um => um.GetRolesAsync(existingUser))
            .ReturnsAsync(new List<string> { "User" });

        var authService = new AuthService(
            mockUserManager.Object,
            mockSignInManager.Object,
            tokenService,
            mockRefreshTokenRepo.Object,
            mockEmailService.Object);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await authService.LoginAsync("test@gmail.com", "CorrectPassword", CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.AccessToken);
        Assert.NotEmpty(result.Data.RefreshToken);
        // ─────────────────────────────────────────────────

        mockRefreshTokenRepo.Verify(
            repo => repo.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ShouldReturnFailure_AndNeverIssueTokens()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockUserManager = CreateMockUserManager();
        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        var mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        var mockEmailService = CreateMockEmailService();
        var tokenService = CreateTestTokenService();

        var existingUser = new IdentityUser { Id = "user-123", Email = "test@gmail.com", EmailConfirmed = true };

        mockUserManager
            .Setup(um => um.FindByEmailAsync("test@gmail.com"))
            .ReturnsAsync(existingUser);

        mockSignInManager
            .Setup(sm => sm.CheckPasswordSignInAsync(existingUser, "WrongPassword", true))
            .ReturnsAsync(SignInResult.Failed);

        var authService = new AuthService(
            mockUserManager.Object,
            mockSignInManager.Object,
            tokenService,
            mockRefreshTokenRepo.Object,
            mockEmailService.Object);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await authService.LoginAsync("test@gmail.com", "WrongPassword", CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email or password", result.ErrorMessage);
        // ─────────────────────────────────────────────────

        mockRefreshTokenRepo.Verify(
            repo => repo.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── NEW TEST — EMAIL NOT CONFIRMED ──────────────────
    // Correct password, but the account was never confirmed —
    // login must still be blocked
    [Fact]
    public async Task LoginAsync_UnconfirmedEmail_ShouldReturnFailure_AndNeverCheckPassword()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockUserManager = CreateMockUserManager();
        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        var mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        var mockEmailService = CreateMockEmailService();
        var tokenService = CreateTestTokenService();

        // ── EmailConfirmed = false ────────────────────────
        var unconfirmedUser = new IdentityUser { Id = "user-456", Email = "unconfirmed@gmail.com", EmailConfirmed = false };
        // ─────────────────────────────────────────────────

        mockUserManager
            .Setup(um => um.FindByEmailAsync("unconfirmed@gmail.com"))
            .ReturnsAsync(unconfirmedUser);

        var authService = new AuthService(
            mockUserManager.Object,
            mockSignInManager.Object,
            tokenService,
            mockRefreshTokenRepo.Object,
            mockEmailService.Object);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await authService.LoginAsync("unconfirmed@gmail.com", "AnyPassword", CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Please confirm your email before logging in", result.ErrorMessage);
        // ─────────────────────────────────────────────────

        // ── VERIFY — PASSWORD WAS NEVER EVEN CHECKED ────
        // Fail fast on the confirmation check before wasting
        // a password verification attempt
        mockSignInManager.Verify(
            sm => sm.CheckPasswordSignInAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
        // ─────────────────────────────────────────────────
    }

    // ══════════════════════════════════════════════════════
    // RefreshTokenAsync TESTS
    // ══════════════════════════════════════════════════════

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ShouldRotateAndReturnNewTokens()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockUserManager = CreateMockUserManager();
        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        var mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        var mockEmailService = CreateMockEmailService();
        var tokenService = CreateTestTokenService();

        var existingUser = new IdentityUser { Id = "user-123", Email = "test@gmail.com", EmailConfirmed = true };

        var oldRefreshToken = new RefreshToken
        {
            Id = 1,
            Token = "old-refresh-token-value",
            UserId = "user-123",
            ExpiresAt = DateTime.UtcNow.AddDays(5),
            IsRevoked = false
        };

        mockRefreshTokenRepo
            .Setup(repo => repo.GetByTokenAsync("old-refresh-token-value", It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldRefreshToken);

        mockUserManager
            .Setup(um => um.FindByIdAsync("user-123"))
            .ReturnsAsync(existingUser);

        mockUserManager
            .Setup(um => um.GetRolesAsync(existingUser))
            .ReturnsAsync(new List<string> { "User" });

        var authService = new AuthService(
            mockUserManager.Object,
            mockSignInManager.Object,
            tokenService,
            mockRefreshTokenRepo.Object,
            mockEmailService.Object);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await authService.RefreshTokenAsync("old-refresh-token-value", CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.AccessToken);
        Assert.NotEmpty(result.Data.RefreshToken);
        Assert.NotEqual("old-refresh-token-value", result.Data.RefreshToken);
        // ─────────────────────────────────────────────────

        mockRefreshTokenRepo.Verify(
            repo => repo.RevokeAsync(oldRefreshToken, It.IsAny<CancellationToken>()),
            Times.Once);

        mockRefreshTokenRepo.Verify(
            repo => repo.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_RevokedToken_ShouldReturnFailure()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockUserManager = CreateMockUserManager();
        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        var mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        var mockEmailService = CreateMockEmailService();
        var tokenService = CreateTestTokenService();

        var revokedToken = new RefreshToken
        {
            Id = 1,
            Token = "already-used-token",
            UserId = "user-123",
            ExpiresAt = DateTime.UtcNow.AddDays(5),
            IsRevoked = true
        };

        mockRefreshTokenRepo
            .Setup(repo => repo.GetByTokenAsync("already-used-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedToken);

        var authService = new AuthService(
            mockUserManager.Object,
            mockSignInManager.Object,
            tokenService,
            mockRefreshTokenRepo.Object,
            mockEmailService.Object);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await authService.RefreshTokenAsync("already-used-token", CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid or expired refresh token", result.ErrorMessage);
        // ─────────────────────────────────────────────────

        mockRefreshTokenRepo.Verify(
            repo => repo.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredToken_ShouldReturnFailure()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockUserManager = CreateMockUserManager();
        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        var mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        var mockEmailService = CreateMockEmailService();
        var tokenService = CreateTestTokenService();

        var expiredToken = new RefreshToken
        {
            Id = 1,
            Token = "expired-token",
            UserId = "user-123",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            IsRevoked = false
        };

        mockRefreshTokenRepo
            .Setup(repo => repo.GetByTokenAsync("expired-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        var authService = new AuthService(
            mockUserManager.Object,
            mockSignInManager.Object,
            tokenService,
            mockRefreshTokenRepo.Object,
            mockEmailService.Object);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await authService.RefreshTokenAsync("expired-token", CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid or expired refresh token", result.ErrorMessage);
        // ─────────────────────────────────────────────────
    }

    // ══════════════════════════════════════════════════════
    // ConfirmEmailAsync TESTS
    // ══════════════════════════════════════════════════════

    [Fact]
    public async Task ConfirmEmailAsync_ValidToken_ShouldSucceed()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockUserManager = CreateMockUserManager();
        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        var mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        var mockEmailService = CreateMockEmailService();
        var tokenService = CreateTestTokenService();

        var user = new IdentityUser { Id = "user-123", Email = "test@gmail.com" };

        mockUserManager
            .Setup(um => um.FindByIdAsync("user-123"))
            .ReturnsAsync(user);

        // ── ENCODE A FAKE TOKEN THE SAME WAY THE REAL CODE DOES ──
        var rawToken = "fake-identity-token";
        var encodedToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
            System.Text.Encoding.UTF8.GetBytes(rawToken));
        // ─────────────────────────────────────────────────────

        mockUserManager
            .Setup(um => um.ConfirmEmailAsync(user, rawToken))
            .ReturnsAsync(IdentityResult.Success);

        var authService = new AuthService(
            mockUserManager.Object,
            mockSignInManager.Object,
            tokenService,
            mockRefreshTokenRepo.Object,
            mockEmailService.Object);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await authService.ConfirmEmailAsync("user-123", encodedToken, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.Contains("confirmed successfully", result.Data);
        // ─────────────────────────────────────────────────
    }

    [Fact]
    public async Task ConfirmEmailAsync_InvalidUserId_ShouldReturnFailure()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockUserManager = CreateMockUserManager();
        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        var mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        var mockEmailService = CreateMockEmailService();
        var tokenService = CreateTestTokenService();

        mockUserManager
            .Setup(um => um.FindByIdAsync("nonexistent-user"))
            .ReturnsAsync((IdentityUser?)null);

        var authService = new AuthService(
            mockUserManager.Object,
            mockSignInManager.Object,
            tokenService,
            mockRefreshTokenRepo.Object,
            mockEmailService.Object);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await authService.ConfirmEmailAsync("nonexistent-user", "any-token", CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid confirmation link", result.ErrorMessage);
        // ─────────────────────────────────────────────────
    }
}