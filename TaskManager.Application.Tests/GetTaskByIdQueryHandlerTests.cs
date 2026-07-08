using Moq;
using TaskManager.Application.Features.Tasks.Queries;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;
using Xunit;

namespace TaskManager.Application.Tests;

public class GetTaskByIdQueryHandlerTests
{
    private const string CurrentUserId = "test-user-id-123";
    private const string OtherUserId = "other-user-id-456";

    [Fact]
    public async Task Handle_WhenCacheHasTask_ReturnsFromCacheWithoutHittingRepository()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        mockCurrentUser.Setup(cu => cu.UserId).Returns(CurrentUserId);

        var cachedTask = new WorkTask { Id = 3, Title = "Cached Task", UserId = CurrentUserId };

        mockCache
            .Setup(cache => cache.GetAsync<WorkTask>("tasks:3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedTask);

        var handler = new GetTaskByIdQueryHandler(mockRepository.Object, mockCache.Object, mockCurrentUser.Object);
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

    [Fact]
    public async Task Handle_WhenCacheIsEmpty_FetchesFromRepositoryAndCachesResult()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        mockCurrentUser.Setup(cu => cu.UserId).Returns(CurrentUserId);

        var dbTask = new WorkTask { Id = 3, Title = "DB Task", UserId = CurrentUserId };

        mockCache
            .Setup(cache => cache.GetAsync<WorkTask>("tasks:3", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkTask?)null);

        mockRepository
            .Setup(repo => repo.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbTask);

        var handler = new GetTaskByIdQueryHandler(mockRepository.Object, mockCache.Object, mockCurrentUser.Object);
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

    [Fact]
    public async Task Handle_TaskNotFoundInCacheOrRepository_ShouldReturnFailure()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        mockCurrentUser.Setup(cu => cu.UserId).Returns(CurrentUserId);

        mockCache
            .Setup(cache => cache.GetAsync<WorkTask>("tasks:999", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkTask?)null);

        mockRepository
            .Setup(repo => repo.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkTask?)null);

        var handler = new GetTaskByIdQueryHandler(mockRepository.Object, mockCache.Object, mockCurrentUser.Object);
        var query = new GetTaskByIdQuery(999);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(query, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Task not found", result.ErrorMessage);
        // ─────────────────────────────────────────────────

        mockCache.Verify(
            cache => cache.SetAsync(It.IsAny<string>(), It.IsAny<WorkTask>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── NEW TEST — OWNERSHIP ON CACHE MISS ──────────────
    // The task genuinely exists in the DB, but belongs to a
    // DIFFERENT user — should return the SAME "Task not found"
    // message as a truly missing task (this is the 404-not-403 behavior)
    [Fact]
    public async Task Handle_TaskExistsButBelongsToAnotherUser_ShouldReturnNotFound()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        mockCurrentUser.Setup(cu => cu.UserId).Returns(CurrentUserId);

        var someoneElsesTask = new WorkTask { Id = 5, Title = "Not Yours", UserId = OtherUserId };

        mockCache
            .Setup(cache => cache.GetAsync<WorkTask>("tasks:5", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkTask?)null);

        mockRepository
            .Setup(repo => repo.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(someoneElsesTask);

        var handler = new GetTaskByIdQueryHandler(mockRepository.Object, mockCache.Object, mockCurrentUser.Object);
        var query = new GetTaskByIdQuery(5);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(query, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        // SAME message as a genuinely missing task — proves
        // no information leaks about the task's existence
        Assert.False(result.IsSuccess);
        Assert.Equal("Task not found", result.ErrorMessage);
        // ─────────────────────────────────────────────────

        // ── VERIFY — NEVER CACHED SOMEONE ELSE'S TASK ────
        // Critical: we must NOT cache this under the current
        // user's context, since it's not actually theirs to see
        mockCache.Verify(
            cache => cache.SetAsync(It.IsAny<string>(), It.IsAny<WorkTask>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
        // ─────────────────────────────────────────────────
    }

    // ── NEW TEST — OWNERSHIP ON CACHE HIT ───────────────
    // Even trickier case: task IS in cache, but the CURRENT
    // requester is a different user than who it was cached for
    [Fact]
    public async Task Handle_CachedTaskBelongsToAnotherUser_ShouldReturnNotFound()
    {
        // ── ARRANGE ────────────────────────────────────
        var mockRepository = new Mock<IWorkTaskRepository>();
        var mockCache = new Mock<ICacheService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        mockCurrentUser.Setup(cu => cu.UserId).Returns(CurrentUserId);

        var cachedTaskOwnedBySomeoneElse = new WorkTask { Id = 7, Title = "Cached But Not Yours", UserId = OtherUserId };

        mockCache
            .Setup(cache => cache.GetAsync<WorkTask>("tasks:7", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedTaskOwnedBySomeoneElse);

        var handler = new GetTaskByIdQueryHandler(mockRepository.Object, mockCache.Object, mockCurrentUser.Object);
        var query = new GetTaskByIdQuery(7);
        // ─────────────────────────────────────────────────

        // ── ACT ────────────────────────────────────────
        var result = await handler.Handle(query, CancellationToken.None);
        // ─────────────────────────────────────────────────

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsSuccess);
        Assert.Equal("Task not found", result.ErrorMessage);
        // ─────────────────────────────────────────────────

        // ── VERIFY — REPOSITORY WAS NEVER CALLED ─────────
        // Since it was a cache "hit" (data existed), we correctly
        // never fell through to the database at all
        mockRepository.Verify(
            repo => repo.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        // ─────────────────────────────────────────────────
    }
}