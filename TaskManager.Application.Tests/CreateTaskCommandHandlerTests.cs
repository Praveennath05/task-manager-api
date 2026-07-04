using Moq;
using TaskManager.Application.Features.Tasks.Commands;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;
using Xunit;

namespace TaskManager.Application.Tests;

// ── TEST CLASS ─────────────────────────────────────────

public class CreateTaskCommandHandlerTests
{
    // ── [Fact] ─────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_CreatesTaskAndReturnsSuccess()
    {
        // ── ARRANGE ────────────────────────────────────

        // Create a FAKE IWorkTaskRepository — no real database involved
        var mockRepository = new Mock<IWorkTaskRepository>();

        // Create a FAKE ICacheService — no real Redis involved
        var mockCache = new Mock<ICacheService>();

        // ── SETUP ────────────────────────────────────────

        mockRepository
            .Setup(repo => repo.CreateAsync(It.IsAny<WorkTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkTask task, CancellationToken ct) => task);
        // ─────────────────────────────────────────────────

        // The handler under test — using the FAKE dependencies, not real ones
        var handler = new CreateTaskCommandHandler(mockRepository.Object, mockCache.Object);

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
        Assert.Equal("Test Task", result.Data!.Title);
        Assert.Equal("Test Description", result.Data.Description);
        // ─────────────────────────────────────────────────

        // ── VERIFY INTERACTIONS ─────────────────────────
        mockCache.Verify(
            cache => cache.RemoveAsync("tasks:all", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}