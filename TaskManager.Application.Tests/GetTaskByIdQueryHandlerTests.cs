
using Moq;
using TaskManager.Application.Features.Tasks.Queries;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;
using Xunit;

namespace TaskManager.Application.Tests;

public class GetTaskByIdQueryHandlerTests
{
    // ── TEST 1 — CACHE HIT ─────────────────────────────
    [Fact]
    public async Task Handle_WhenCacheHasTask_ReturnsFromCacheWithoutHittingRepository()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();

        var cachedTask = new WorkTask { Id = 3, Title = "Cached Task" };

        mockCache
            .Setup(cache => cache.GetAsync<WorkTask>("tasks:3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedTask);

        var handler = new GetTaskByIdQueryHandler(mockRepository.Object, mockCache.Object);
        var query = new GetTaskByIdQuery(3);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(query, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Cached Task", result.Data.Title);
        // ─────────────────────────────────────────────────

        mockRepository.Verify(
            repo => repo.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── TEST 2 — CACHE MISS, TASK EXISTS ────────────────
    [Fact]
    public async Task Handle_WhenCacheIsEmpty_FetchesFromRepositoryAndCachesResult()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();

        var dbTask = new WorkTask { Id = 3, Title = "DB Task" };

        mockCache
            .Setup(cache => cache.GetAsync<WorkTask>("tasks:3", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkTask?)null);

        mockRepository
            .Setup(repo => repo.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbTask);

        var handler = new GetTaskByIdQueryHandler(mockRepository.Object, mockCache.Object);
        var query = new GetTaskByIdQuery(3);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(query, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("DB Task", result.Data.Title);
        // ─────────────────────────────────────────────────

        mockRepository.Verify(
            repo => repo.GetByIdAsync(3, It.IsAny<CancellationToken>()),
            Times.Once);

        mockCache.Verify(
            cache => cache.SetAsync("tasks:3", dbTask, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── TEST 3 — TASK DOES NOT EXIST ANYWHERE ───────────
    // Neither cache NOR database has this task
    [Fact]
    public async Task Handle_TaskNotFoundInCacheOrRepository_ShouldReturnFailure()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();

        mockCache
            .Setup(cache => cache.GetAsync<WorkTask>("tasks:999", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkTask?)null);

        mockRepository
            .Setup(repo => repo.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkTask?)null);

        var handler = new GetTaskByIdQueryHandler(mockRepository.Object, mockCache.Object);
        var query = new GetTaskByIdQuery(999);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(query, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Task not found", result.ErrorMessage);
        // ─────────────────────────────────────────────────

        // ── VERIFY — NOTHING GETS CACHED FOR A MISSING TASK ──
        // Important: don't cache a "null result" — that could
        // mask a task being created moments later
        mockCache.Verify(
            cache => cache.SetAsync(It.IsAny<string>(), It.IsAny<WorkTask>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
        // ─────────────────────────────────────────────────
    }
}