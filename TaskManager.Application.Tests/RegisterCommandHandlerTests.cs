using Moq;
using TaskManager.Application.Features.Auth.Commands;
using TaskManager.Domain.Common;
using TaskManager.Domain.Interfaces;
using Xunit;

namespace TaskManager.Application.Tests;

public class RegisterCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ShouldCallAuthService_AndReturnSuccess()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockAuthService = new Mock<IAuthService>();

        mockAuthService
            .Setup(auth => auth.RegisterAsync("test@gmail.com", "Password123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("Registration successful"));

        var handler = new RegisterCommandHandler(mockAuthService.Object);
        var command = new RegisterCommand("test@gmail.com", "Password123");
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(command, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.Equal("Registration successful", result.Data);
        // ─────────────────────────────────────────────────

        // ── VERIFY — HANDLER DELEGATED CORRECTLY ────────
        // Proves the handler passed the EXACT email/password through
        // without modifying them — a simple but real thing to verify
        mockAuthService.Verify(
            auth => auth.RegisterAsync("test@gmail.com", "Password123", It.IsAny<CancellationToken>()),
            Times.Once);
        // ─────────────────────────────────────────────────
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ShouldReturnFailure()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockAuthService = new Mock<IAuthService>();

        mockAuthService
            .Setup(auth => auth.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Failure("Email already registered"));

        var handler = new RegisterCommandHandler(mockAuthService.Object);
        var command = new RegisterCommand("existing@gmail.com", "Password123");
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(command, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Email already registered", result.ErrorMessage);
        // ─────────────────────────────────────────────────
    }
}