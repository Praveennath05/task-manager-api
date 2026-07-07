using Moq;
using TaskManager.Application.Features.Auth.Commands;
using TaskManager.Domain.Common;
using TaskManager.Domain.Interfaces;
using Xunit;

namespace TaskManager.Application.Tests;

public class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCredentials_ShouldReturnAuthResult()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockAuthService = new Mock<IAuthService>();

        var expectedAuthResult = new AuthResult
        {
            AccessToken = "fake-access-token",
            RefreshToken = "fake-refresh-token"
        };

        mockAuthService
            .Setup(auth => auth.LoginAsync("test@gmail.com", "Password123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResult>.Success(expectedAuthResult));

        var handler = new LoginCommandHandler(mockAuthService.Object);
        var command = new LoginCommand("test@gmail.com", "Password123");
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(command, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("fake-access-token", result.Data.AccessToken);
        Assert.Equal("fake-refresh-token", result.Data.RefreshToken);
        // ─────────────────────────────────────────────────

        mockAuthService.Verify(
            auth => auth.LoginAsync("test@gmail.com", "Password123", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidCredentials_ShouldReturnFailure()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockAuthService = new Mock<IAuthService>();

        mockAuthService
            .Setup(auth => auth.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResult>.Failure("Invalid email or password"));

        var handler = new LoginCommandHandler(mockAuthService.Object);
        var command = new LoginCommand("test@gmail.com", "WrongPassword");
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(command, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email or password", result.ErrorMessage);
        // ─────────────────────────────────────────────────
    }
}