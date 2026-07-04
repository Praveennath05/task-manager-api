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

        mockRepository
            .Setup(repo => repo.CreateAsync(It.IsAny<WorkTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkTask task, CancellationToken ct) => task);

        var handler = new CreateTaskCommandHandler(mockRepository.Object, mockCache.Object);

        var command = new CreateTaskCommand(
            Title: "Test Task",
            Description: "Test Description",
            DueDate: DateTime.UtcNow.AddDays(1));

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(command, CancellationToken.None);

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Test Task", result.Data.Title);
        Assert.Equal("Test Description", result.Data.Description);

        // ── VERIFY — REPOSITORY WAS ACTUALLY CALLED ─────
        mockRepository.Verify(
            repo => repo.CreateAsync(It.IsAny<WorkTask>(), It.IsAny<CancellationToken>()),
            Times.Once);

        // ── VERIFY — CACHE WAS INVALIDATED ──────────────
        mockCache.Verify(
            cache => cache.RemoveAsync("tasks:all", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}