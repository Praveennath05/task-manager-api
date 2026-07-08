using Moq;
using TaskManager.Application.Features.Tasks.Commands;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;
using Xunit;

namespace TaskManager.Application.Tests;

public class CreateTaskCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateTask_AndReturnSuccess()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        // ── SIMULATE A LOGGED-IN USER ────────────────────
        mockCurrentUser
            .Setup(cu => cu.UserId)
            .Returns("test-user-id-123");
        // ─────────────────────────────────────────────────

        mockRepository
            .Setup(repo => repo.CreateAsync(It.IsAny<WorkTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkTask task, CancellationToken ct) => task);

        var handler = new CreateTaskCommandHandler(
            mockRepository.Object,
            mockCache.Object,
            mockCurrentUser.Object);

        var command = new CreateTaskCommand(
            Title: "Test Task",
            Description: "Test Description",
            DueDate: DateTime.UtcNow.AddDays(1));
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(command, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Test Task", result.Data.Title);
        Assert.Equal("Test Description", result.Data.Description);

        // ── VERIFY — TASK WAS STAMPED WITH CORRECT USER ──
        // Proves the ownership feature we just built actually works
        Assert.Equal("test-user-id-123", result.Data.UserId);
        // ─────────────────────────────────────────────────

        mockRepository.Verify(
            repo => repo.CreateAsync(It.IsAny<WorkTask>(), It.IsAny<CancellationToken>()),
            Times.Once);

mockCache.Verify(
    cache => cache.RemoveAsync("tasks:all:test-user-id-123", It.IsAny<CancellationToken>()),
    Times.Once);    }

    // ── NEW TEST — MISSING USER ID ─────────────────────
    // Tests the defensive check we added — what if somehow
    // there's no logged-in user (shouldn't happen due to
    // [Authorize], but good practice to verify the guard works)
    [Fact]
    public async Task Handle_NoCurrentUser_ShouldReturnFailure()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        mockCurrentUser
            .Setup(cu => cu.UserId)
            .Returns((string?)null);

        var handler = new CreateTaskCommandHandler(
            mockRepository.Object,
            mockCache.Object,
            mockCurrentUser.Object);

        var command = new CreateTaskCommand("Test Task", "Test Description", null);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(command, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Unable to determine current user", result.ErrorMessage);

        mockRepository.Verify(
            repo => repo.CreateAsync(It.IsAny<WorkTask>(), It.IsAny<CancellationToken>()),
            Times.Never);
        // ─────────────────────────────────────────────────
    }
}