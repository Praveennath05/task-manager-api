using Moq;
using TaskManager.Application.Features.Tasks.Queries;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;
using Xunit;

namespace TaskManager.Application.Tests;

public class GetAllTasksQueryHandlerTests
{
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

        mockCache
            .Setup(cache => cache.GetAsync<List<WorkTask>>("tasks:all", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedTasks);

        var handler = new GetAllTasksQueryHandler(mockRepository.Object, mockCache.Object);
        var query = new GetAllTasksQuery();

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(query, CancellationToken.None);

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal("Cached Task", result.Data[0].Title);

        mockRepository.Verify(
            repo => repo.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

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

        mockCache
            .Setup(cache => cache.GetAsync<List<WorkTask>>("tasks:all", It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<WorkTask>?)null);

        mockRepository
            .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(databaseTasks);

        var handler = new GetAllTasksQueryHandler(mockRepository.Object, mockCache.Object);
        var query = new GetAllTasksQuery();

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(query, CancellationToken.None);

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("DB Task", result.Data[0].Title);

        mockRepository.Verify(
            repo => repo.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        mockCache.Verify(
            cache => cache.SetAsync(
                "tasks:all",
                databaseTasks,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}