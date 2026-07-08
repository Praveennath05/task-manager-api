using Moq;
using TaskManager.Application.Features.Tasks.Commands;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;
using Xunit;

namespace TaskManager.Application.Tests;

public class DeleteTaskCommandHandlerTests
{
    private const string CurrentUserId = "test-user-id-123";
    private const string OtherUserId = "other-user-id-456";

    [Fact]
    public async Task Handle_ExistingTask_ShouldDeleteAndReturnSuccess()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        mockCurrentUser.Setup(cu => cu.UserId).Returns(CurrentUserId);

        var existingTask = new WorkTask { Id = 5, Title = "Task To Delete", UserId = CurrentUserId };

        mockRepository
            .Setup(repo => repo.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTask);

        var handler = new DeleteTaskCommandHandler(mockRepository.Object, mockCache.Object, mockCurrentUser.Object);
        var command = new DeleteTaskCommand(5);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(command, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        // ─────────────────────────────────────────────────

        mockRepository.Verify(
            repo => repo.DeleteAsync(5, It.IsAny<CancellationToken>()),
            Times.Once);

        mockCache.Verify(
            cache => cache.RemoveAsync("tasks:all", It.IsAny<CancellationToken>()),
            Times.Once);

        mockCache.Verify(
            cache => cache.RemoveAsync("tasks:5", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentTask_ShouldReturnFailure_AndNeverCallDelete()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        mockCurrentUser.Setup(cu => cu.UserId).Returns(CurrentUserId);

        mockRepository
            .Setup(repo => repo.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkTask?)null);

        var handler = new DeleteTaskCommandHandler(mockRepository.Object, mockCache.Object, mockCurrentUser.Object);
        var command = new DeleteTaskCommand(999);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(command, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Task not found", result.ErrorMessage);
        // ─────────────────────────────────────────────────

        mockRepository.Verify(
            repo => repo.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);

        mockCache.Verify(
            cache => cache.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── NEW TEST — OWNERSHIP ────────────────────────────
    [Fact]
    public async Task Handle_TaskBelongsToAnotherUser_ShouldReturnNotFound_AndNeverDelete()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        mockCurrentUser.Setup(cu => cu.UserId).Returns(CurrentUserId);

        var someoneElsesTask = new WorkTask { Id = 5, Title = "Not Yours", UserId = OtherUserId };

        mockRepository
            .Setup(repo => repo.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(someoneElsesTask);

        var handler = new DeleteTaskCommandHandler(mockRepository.Object, mockCache.Object, mockCurrentUser.Object);
        var command = new DeleteTaskCommand(5);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(command, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Task not found", result.ErrorMessage);
        // ─────────────────────────────────────────────────

        // ── VERIFY — TASK WAS NEVER ACTUALLY DELETED ────
        // The most important assertion in this whole file:
        // even though the task was found, a different user's
        // task must NEVER actually be deleted
        mockRepository.Verify(
            repo => repo.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);

        mockCache.Verify(
            cache => cache.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        // ─────────────────────────────────────────────────
    }
}