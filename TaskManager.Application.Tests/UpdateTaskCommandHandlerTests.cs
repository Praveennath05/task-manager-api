using Moq;
using TaskManager.Application.Features.Tasks.Commands;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;
using Xunit;

namespace TaskManager.Application.Tests;

public class UpdateTaskCommandHandlerTests
{
    private const string CurrentUserId = "test-user-id-123";
    private const string OtherUserId = "other-user-id-456";

    [Fact]
    public async Task Handle_ExistingTask_ShouldUpdateFieldsCorrectly()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        mockCurrentUser.Setup(cu => cu.UserId).Returns(CurrentUserId);

        var existingTask = new WorkTask
        {
            Id = 5,
            Title = "Old Title",
            Description = "Old Description",
            IsCompleted = false,
            DueDate = DateTime.UtcNow.AddDays(1),
            UserId = CurrentUserId
        };

        mockRepository
            .Setup(repo => repo.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTask);

        mockRepository
            .Setup(repo => repo.UpdateAsync(It.IsAny<WorkTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkTask task, CancellationToken ct) => task);

        var handler = new UpdateTaskCommandHandler(mockRepository.Object, mockCache.Object, mockCurrentUser.Object);

        var command = new UpdateTaskCommand(
            Id: 5,
            Title: "New Title",
            Description: "New Description",
            IsCompleted: true,
            DueDate: DateTime.UtcNow.AddDays(2));
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(command, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("New Title", result.Data.Title);
        Assert.Equal("New Description", result.Data.Description);
        Assert.True(result.Data.IsCompleted);
        Assert.NotNull(result.Data.UpdatedAt);
        // ─────────────────────────────────────────────────

        mockRepository.Verify(
            repo => repo.UpdateAsync(It.IsAny<WorkTask>(), It.IsAny<CancellationToken>()),
            Times.Once);

        mockCache.Verify(
            cache => cache.RemoveAsync("tasks:all", It.IsAny<CancellationToken>()),
            Times.Once);

        mockCache.Verify(
            cache => cache.RemoveAsync("tasks:5", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentTask_ShouldReturnFailure_AndNeverCallUpdate()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        mockCurrentUser.Setup(cu => cu.UserId).Returns(CurrentUserId);

        mockRepository
            .Setup(repo => repo.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkTask?)null);

        var handler = new UpdateTaskCommandHandler(mockRepository.Object, mockCache.Object, mockCurrentUser.Object);

        var command = new UpdateTaskCommand(
            Id: 999,
            Title: "Doesn't Matter",
            Description: "Doesn't Matter",
            IsCompleted: false,
            DueDate: null);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(command, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Task not found", result.ErrorMessage);
        // ─────────────────────────────────────────────────

        mockRepository.Verify(
            repo => repo.UpdateAsync(It.IsAny<WorkTask>(), It.IsAny<CancellationToken>()),
            Times.Never);

        mockCache.Verify(
            cache => cache.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── NEW TEST — OWNERSHIP ────────────────────────────
    // The task exists, but belongs to a DIFFERENT user —
    // must be rejected, same message as "not found"
    [Fact]
    public async Task Handle_TaskBelongsToAnotherUser_ShouldReturnNotFound_AndNeverUpdate()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        mockCurrentUser.Setup(cu => cu.UserId).Returns(CurrentUserId);

        var someoneElsesTask = new WorkTask
        {
            Id = 5,
            Title = "Not Yours",
            UserId = OtherUserId
        };

        mockRepository
            .Setup(repo => repo.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(someoneElsesTask);

        var handler = new UpdateTaskCommandHandler(mockRepository.Object, mockCache.Object, mockCurrentUser.Object);

        var command = new UpdateTaskCommand(
            Id: 5,
            Title: "Trying To Hijack",
            Description: "Malicious Update",
            IsCompleted: true,
            DueDate: null);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(command, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Task not found", result.ErrorMessage);
        // ─────────────────────────────────────────────────

        // ── VERIFY — TASK WAS NEVER ACTUALLY MODIFIED ───
        // This is the critical security assertion: even though
        // we found the task, we must NEVER call UpdateAsync
        // for a task that doesn't belong to the requester
        mockRepository.Verify(
            repo => repo.UpdateAsync(It.IsAny<WorkTask>(), It.IsAny<CancellationToken>()),
            Times.Never);

        mockCache.Verify(
            cache => cache.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        // ─────────────────────────────────────────────────
    }
}