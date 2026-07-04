using Moq;
using TaskManager.Application.Features.Tasks.Commands;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;
using Xunit;

namespace TaskManager.Application.Tests;

public class UpdateTaskCommandHandlerTests
{
    // ── TEST 1 — TASK EXISTS ────────────────────────────
    [Fact]
    public async Task Handle_ExistingTask_ShouldUpdateFieldsCorrectly()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();

        // The task as it currently exists in the "database"
        var existingTask = new WorkTask
        {
            Id = 5,
            Title = "Old Title",
            Description = "Old Description",
            IsCompleted = false,
            DueDate = DateTime.UtcNow.AddDays(1)
        };

        mockRepository
            .Setup(repo => repo.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTask);

        // Simulate: UpdateAsync just returns whatever was passed in
        mockRepository
            .Setup(repo => repo.UpdateAsync(It.IsAny<WorkTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkTask task, CancellationToken ct) => task);

        var handler = new UpdateTaskCommandHandler(mockRepository.Object, mockCache.Object);

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

        // ── ASSERT — PROVE THE ACTUAL FIELDS CHANGED ────
        // This is the key thing to verify — not just "it succeeded,"
        // but that EVERY field was actually updated to the new value
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("New Title", result.Data.Title);
        Assert.Equal("New Description", result.Data.Description);
        Assert.True(result.Data.IsCompleted);
        // ─────────────────────────────────────────────────

        // ── VERIFY — UpdatedAt TIMESTAMP WAS SET ────────
        // Proves the handler doesn't forget to stamp when the change happened
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

    // ── TEST 2 — TASK DOES NOT EXIST ────────────────────
    [Fact]
    public async Task Handle_NonExistentTask_ShouldReturnFailure_AndNeverCallUpdate()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();

        mockRepository
            .Setup(repo => repo.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkTask?)null);

        var handler = new UpdateTaskCommandHandler(mockRepository.Object, mockCache.Object);

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
}