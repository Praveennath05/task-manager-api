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
        var mockCurrentUser = new Mock<ICurrentUserService>();

        const string userId = "test-user-id-123";
        mockCurrentUser.Setup(cu => cu.UserId).Returns(userId);

        var cachedTasks = new List<WorkTask>
        {
            new WorkTask { Id = 1, Title = "Cached Task", UserId = userId }
        };

        // ── PER-USER CACHE KEY IN THE MOCK SETUP ─────────
        mockCache
            .Setup(cache => cache.GetAsync<List<WorkTask>>($"tasks:all:{userId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedTasks);
        // ─────────────────────────────────────────────────

        var handler = new GetAllTasksQueryHandler(mockRepository.Object, mockCache.Object, mockCurrentUser.Object);
        var query = new GetAllTasksQuery();
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(query, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal("Cached Task", result.Data[0].Title);
        // ─────────────────────────────────────────────────

        mockRepository.Verify(
            repo => repo.GetAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCacheIsEmpty_FetchesFromRepositoryAndCachesResult()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        const string userId = "test-user-id-123";
        mockCurrentUser.Setup(cu => cu.UserId).Returns(userId);

        var databaseTasks = new List<WorkTask>
        {
            new WorkTask { Id = 1, Title = "DB Task", UserId = userId }
        };

        mockCache
            .Setup(cache => cache.GetAsync<List<WorkTask>>($"tasks:all:{userId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<WorkTask>?)null);

        // ── REPOSITORY NOW TAKES userId AS FIRST ARGUMENT ──
        mockRepository
            .Setup(repo => repo.GetAllAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(databaseTasks);
        // ─────────────────────────────────────────────────

        var handler = new GetAllTasksQueryHandler(mockRepository.Object, mockCache.Object, mockCurrentUser.Object);
        var query = new GetAllTasksQuery();
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(query, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("DB Task", result.Data[0].Title);
        // ─────────────────────────────────────────────────

        mockRepository.Verify(
            repo => repo.GetAllAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);

        mockCache.Verify(
            cache => cache.SetAsync($"tasks:all:{userId}", databaseTasks, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── NEW TEST — MISSING USER ─────────────────────────
    [Fact]
    public async Task Handle_NoCurrentUser_ShouldReturnFailure()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        mockCurrentUser.Setup(cu => cu.UserId).Returns((string?)null);

        var handler = new GetAllTasksQueryHandler(mockRepository.Object, mockCache.Object, mockCurrentUser.Object);
        var query = new GetAllTasksQuery();
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(query, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Unable to determine current user", result.ErrorMessage);

        mockRepository.Verify(
            repo => repo.GetAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        // ─────────────────────────────────────────────────
    }
}