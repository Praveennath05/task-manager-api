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
    // ─────────────────────────────────────────────────────

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
        // ─────────────────────────────────────────────────
    }
}