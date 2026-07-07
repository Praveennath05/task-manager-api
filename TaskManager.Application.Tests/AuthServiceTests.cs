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
    // ── HELPER METHOD ──────────────────────────────────────
    // UserManager<IdentityUser> has a complex constructor —
    // it needs a IUserStore<IdentityUser> plus several optional
    // services. Moq can create a "fake" UserManager by mocking
    // just the IUserStore and passing nulls for the rest (Identity
    // tolerates nulls for options/validators/etc in this pattern —
    // this is the standard, well-documented way to test Identity code)
    private static Mock<UserManager<IdentityUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    // ── HELPER METHOD ──────────────────────────────────────
    // SignInManager<IdentityUser> similarly needs several
    // constructor arguments — UserManager, HttpContextAccessor,
    // ClaimsPrincipalFactory, Options, Logger, Schemes, Confirmation
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

    // ── HELPER METHOD ──────────────────────────────────────
    // Extracted since every test needs a working TokenService —
    // avoids repeating this configuration setup in every single test
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

    [Fact]
    public async Task RegisterAsync_NewEmail_ShouldCreateUserAndReturnSuccess()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockUserManager = CreateMockUserManager();
        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        var mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        var mockLogger = new Mock<ILogger<TokenService>>();

        // Fake config just enough for TokenService to not crash
        var configValues = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey", "ThisIsAVerySecretKeyForTestingOnly12345678" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" },
            { "JwtSettings:ExpiryMinutes", "60" }
        };
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();
        var tokenService = new TokenService(configuration);

        // Simulate: no existing user with this email
        mockUserManager
            .Setup(um => um.FindByEmailAsync("new@gmail.com"))
            .ReturnsAsync((IdentityUser?)null);

        // Simulate: user creation succeeds
        mockUserManager
            .Setup(um => um.CreateAsync(It.IsAny<IdentityUser>(), "Password123"))
            .ReturnsAsync(IdentityResult.Success);

        mockUserManager
            .Setup(um => um.AddToRoleAsync(It.IsAny<IdentityUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        var authService = new AuthService(
            mockUserManager.Object,
            mockSignInManager.Object,
            tokenService,
            mockRefreshTokenRepo.Object);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await authService.RegisterAsync("new@gmail.com", "Password123", CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.Equal("Registration successful", result.Data);
        // ─────────────────────────────────────────────────

        mockUserManager.Verify(
            um => um.CreateAsync(It.IsAny<IdentityUser>(), "Password123"),
            Times.Once);

        mockUserManager.Verify(
            um => um.AddToRoleAsync(It.IsAny<IdentityUser>(), "User"),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ShouldReturnFailure_AndNeverCallCreate()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockUserManager = CreateMockUserManager();
        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        var mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();

        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "JwtSettings:SecretKey", "ThisIsAVerySecretKeyForTestingOnly12345678" },
                { "JwtSettings:Issuer", "TestIssuer" },
                { "JwtSettings:Audience", "TestAudience" },
                { "JwtSettings:ExpiryMinutes", "60" }
            })
            .Build();
        var tokenService = new TokenService(configuration);

        var existingUser = new IdentityUser { Email = "existing@gmail.com" };

        // Simulate: email ALREADY exists
        mockUserManager
            .Setup(um => um.FindByEmailAsync("existing@gmail.com"))
            .ReturnsAsync(existingUser);

        var authService = new AuthService(
            mockUserManager.Object,
            mockSignInManager.Object,
            tokenService,
            mockRefreshTokenRepo.Object);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await authService.RegisterAsync("existing@gmail.com", "Password123", CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Email already registered", result.ErrorMessage);
        // ─────────────────────────────────────────────────

        // ── VERIFY — NEVER ATTEMPTED TO CREATE A DUPLICATE ──
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
        var tokenService = CreateTestTokenService();

        var existingUser = new IdentityUser { Id = "user-123", Email = "test@gmail.com" };

        mockUserManager
            .Setup(um => um.FindByEmailAsync("test@gmail.com"))
            .ReturnsAsync(existingUser);

        // ── SIGNINMANAGER MOCK ──────────────────────────
        // CheckPasswordSignInAsync verifies the password —
        // simulate it succeeding
        mockSignInManager
            .Setup(sm => sm.CheckPasswordSignInAsync(existingUser, "CorrectPassword", true))
            .ReturnsAsync(SignInResult.Success);
        // ─────────────────────────────────────────────────

        mockUserManager
            .Setup(um => um.GetRolesAsync(existingUser))
            .ReturnsAsync(new List<string> { "User" });

        var authService = new AuthService(
            mockUserManager.Object,
            mockSignInManager.Object,
            tokenService,
            mockRefreshTokenRepo.Object);
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

        // ── VERIFY — REFRESH TOKEN WAS ACTUALLY SAVED ───
        // Proves LoginAsync doesn't just generate a random string —
        // it persists the refresh token via the repository, exactly
        // like the real flow we manually tested with curl earlier
        mockRefreshTokenRepo.Verify(
            repo => repo.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Once);
        // ─────────────────────────────────────────────────
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ShouldReturnFailure_AndNeverIssueTokens()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockUserManager = CreateMockUserManager();
        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        var mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        var tokenService = CreateTestTokenService();

        var existingUser = new IdentityUser { Id = "user-123", Email = "test@gmail.com" };

        mockUserManager
            .Setup(um => um.FindByEmailAsync("test@gmail.com"))
            .ReturnsAsync(existingUser);

        // ── SIMULATE WRONG PASSWORD ──────────────────────
        mockSignInManager
            .Setup(sm => sm.CheckPasswordSignInAsync(existingUser, "WrongPassword", true))
            .ReturnsAsync(SignInResult.Failed);
        // ─────────────────────────────────────────────────

        var authService = new AuthService(
            mockUserManager.Object,
            mockSignInManager.Object,
            tokenService,
            mockRefreshTokenRepo.Object);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await authService.LoginAsync("test@gmail.com", "WrongPassword", CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email or password", result.ErrorMessage);
        // ─────────────────────────────────────────────────

        // ── VERIFY — NO TOKEN WAS EVER CREATED ──────────
        // Critical security check: a failed login must NEVER
        // result in a refresh token being generated and saved
        mockRefreshTokenRepo.Verify(
            repo => repo.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
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
        var tokenService = CreateTestTokenService();

        var existingUser = new IdentityUser { Id = "user-123", Email = "test@gmail.com" };

        // ── SIMULATE A VALID, ACTIVE REFRESH TOKEN ──────
        var oldRefreshToken = new RefreshToken
        {
            Id = 1,
            Token = "old-refresh-token-value",
            UserId = "user-123",
            ExpiresAt = DateTime.UtcNow.AddDays(5), // still valid
            IsRevoked = false
        };
        // ─────────────────────────────────────────────────

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
            mockRefreshTokenRepo.Object);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await authService.RefreshTokenAsync("old-refresh-token-value", CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.AccessToken);
        Assert.NotEmpty(result.Data.RefreshToken);

        // ── PROVE ROTATION — NEW TOKEN IS DIFFERENT ─────
        Assert.NotEqual("old-refresh-token-value", result.Data.RefreshToken);
        // ─────────────────────────────────────────────────

        // ── VERIFY — OLD TOKEN WAS REVOKED ──────────────
        // This is the exact security guarantee we manually
        // proved earlier with curl — now automated
        mockRefreshTokenRepo.Verify(
            repo => repo.RevokeAsync(oldRefreshToken, It.IsAny<CancellationToken>()),
            Times.Once);
        // ─────────────────────────────────────────────────

        // ── VERIFY — NEW TOKEN WAS SAVED ─────────────────
        mockRefreshTokenRepo.Verify(
            repo => repo.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Once);
        // ─────────────────────────────────────────────────
    }

    [Fact]
    public async Task RefreshTokenAsync_RevokedToken_ShouldReturnFailure()
    {
        // ── ARRANGE ────────────────────────────────────
        // This is the exact scenario we manually tested with curl:
        // reusing an already-used (revoked) refresh token
        var mockUserManager = CreateMockUserManager();
        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        var mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        var tokenService = CreateTestTokenService();

        var revokedToken = new RefreshToken
        {
            Id = 1,
            Token = "already-used-token",
            UserId = "user-123",
            ExpiresAt = DateTime.UtcNow.AddDays(5),
            IsRevoked = true // ALREADY revoked — this is the key condition
        };
        // ─────────────────────────────────────────────────

        mockRefreshTokenRepo
            .Setup(repo => repo.GetByTokenAsync("already-used-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedToken);

        var authService = new AuthService(
            mockUserManager.Object,
            mockSignInManager.Object,
            tokenService,
            mockRefreshTokenRepo.Object);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await authService.RefreshTokenAsync("already-used-token", CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid or expired refresh token", result.ErrorMessage);
        // ─────────────────────────────────────────────────

        // ── VERIFY — NO NEW TOKEN WAS ISSUED ────────────
        mockRefreshTokenRepo.Verify(
            repo => repo.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never);
        // ─────────────────────────────────────────────────
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredToken_ShouldReturnFailure()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockUserManager = CreateMockUserManager();
        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        var mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        var tokenService = CreateTestTokenService();

        var expiredToken = new RefreshToken
        {
            Id = 1,
            Token = "expired-token",
            UserId = "user-123",
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // expired YESTERDAY
            IsRevoked = false
        };
        // ─────────────────────────────────────────────────

        mockRefreshTokenRepo
            .Setup(repo => repo.GetByTokenAsync("expired-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        var authService = new AuthService(
            mockUserManager.Object,
            mockSignInManager.Object,
            tokenService,
            mockRefreshTokenRepo.Object);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await authService.RefreshTokenAsync("expired-token", CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid or expired refresh token", result.ErrorMessage);
        // ─────────────────────────────────────────────────
    }

}