using Moq;
using TaskManager.Application.Features.Tasks.Commands;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;
using Xunit;

namespace TaskManager.Application.Tests;

public class DeleteTaskCommandHandlerTests
{
    // ── TEST 1 — TASK EXISTS ────────────────────────────
    [Fact]
    public async Task Handle_ExistingTask_ShouldDeleteAndReturnSuccess()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();

        var existingTask = new WorkTask { Id = 5, Title = "Task To Delete" };

        // Simulate: repository finds the task when asked
        mockRepository
            .Setup(repo => repo.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTask);

        var handler = new DeleteTaskCommandHandler(mockRepository.Object, mockCache.Object);
        var command = new DeleteTaskCommand(5);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(command, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        // ─────────────────────────────────────────────────

        // ── VERIFY — ACTUAL DELETE WAS CALLED ───────────
        mockRepository.Verify(
            repo => repo.DeleteAsync(5, It.IsAny<CancellationToken>()),
            Times.Once);
        // ─────────────────────────────────────────────────

        // ── VERIFY — BOTH CACHE KEYS INVALIDATED ────────
        // We fixed this earlier — delete must clear BOTH
        // "tasks:all" AND "tasks:5" specifically
        mockCache.Verify(
            cache => cache.RemoveAsync("tasks:all", It.IsAny<CancellationToken>()),
            Times.Once);

        mockCache.Verify(
            cache => cache.RemoveAsync("tasks:5", It.IsAny<CancellationToken>()),
            Times.Once);
        // ─────────────────────────────────────────────────
    }

    // ── TEST 2 — TASK DOES NOT EXIST ────────────────────
    // This is the branch we haven't tested anywhere yet
    [Fact]
    public async Task Handle_NonExistentTask_ShouldReturnFailure_AndNeverCallDelete()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();

        // Simulate: repository finds NOTHING — task doesn't exist
        mockRepository
            .Setup(repo => repo.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkTask?)null);

        var handler = new DeleteTaskCommandHandler(mockRepository.Object, mockCache.Object);
        var command = new DeleteTaskCommand(999);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(command, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Task not found", result.ErrorMessage);
        // ─────────────────────────────────────────────────

        // ── VERIFY — CRITICAL SAFETY CHECK ──────────────
        // If the task doesn't exist, DeleteAsync must NEVER be called
        // This proves the handler correctly checks existence BEFORE
        // attempting to delete — no wasted database calls, no silent
        // failures pretending something was deleted when it wasn't
        mockRepository.Verify(
            repo => repo.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        // ─────────────────────────────────────────────────

        // ── VERIFY — CACHE SHOULD NOT BE TOUCHED ────────
        // Nothing changed in the database, so nothing should
        // be invalidated in the cache either
        mockCache.Verify(
            cache => cache.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        // ─────────────────────────────────────────────────
    }
}