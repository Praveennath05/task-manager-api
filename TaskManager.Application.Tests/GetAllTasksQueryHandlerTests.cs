using Moq;
using TaskManager.Application.Features.Tasks.Queries;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;
using Xunit;

namespace TaskManager.Application.Tests;

public class GetAllTasksQueryHandlerTests
{
    // ── TEST 1 — CACHE HIT ─────────────────────────────
    // If data is already in the cache, the handler should
    // return it WITHOUT ever touching the repository/database
    [Fact]
    public async Task Handle_WhenCacheHasData_ReturnsFromCacheWithoutHittingRepository()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();

        var cachedTasks = new List<WorkTask>
        {
            new WorkTask { Id = 1, Title = "Cached Task" }
        };

        // Simulate: cache ALREADY has data for "tasks:all"
        mockCache
            .Setup(cache => cache.GetAsync<List<WorkTask>>("tasks:all", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedTasks);

        var handler = new GetAllTasksQueryHandler(mockRepository.Object, mockCache.Object);
        var query = new GetAllTasksQuery();
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(query, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
        Assert.Equal("Cached Task", result.Data![0].Title);
        // ─────────────────────────────────────────────────

        // ── VERIFY — THE KEY ASSERTION ──────────────────
        // Prove the repository (database) was NEVER called
        // This is the actual point of cache-aside — skip the DB entirely
        mockRepository.Verify(
            repo => repo.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        // ─────────────────────────────────────────────────
    }

    // ── TEST 2 — CACHE MISS ────────────────────────────
    // If cache is empty, handler should fall back to the
    // repository AND store the result in cache afterward
    [Fact]
    public async Task Handle_WhenCacheIsEmpty_FetchesFromRepositoryAndCachesResult()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();

        var databaseTasks = new List<WorkTask>
        {
            new WorkTask { Id = 1, Title = "DB Task" }
        };

        // Simulate: cache is EMPTY (returns null — a "miss")
        mockCache
            .Setup(cache => cache.GetAsync<List<WorkTask>>("tasks:all", It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<WorkTask>?)null);

        // Simulate: repository returns real data
        mockRepository
            .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(databaseTasks);

        var handler = new GetAllTasksQueryHandler(mockRepository.Object, mockCache.Object);
        var query = new GetAllTasksQuery();
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(query, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.Equal("DB Task", result.Data![0].Title);
        // ─────────────────────────────────────────────────

        // ── VERIFY — REPOSITORY WAS CALLED ──────────────
        mockRepository.Verify(
            repo => repo.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        // ─────────────────────────────────────────────────

        // ── VERIFY — RESULT WAS CACHED FOR NEXT TIME ────
        mockCache.Verify(
            cache => cache.SetAsync(
                "tasks:all",
                databaseTasks,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        // ─────────────────────────────────────────────────
    }
}