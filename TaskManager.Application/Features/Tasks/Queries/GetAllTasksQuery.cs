using MediatR;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Features.Tasks.Queries;

public record GetAllTasksQuery() : IRequest<Result<List<WorkTask>>>;

public class GetAllTasksQueryHandler : IRequestHandler<GetAllTasksQuery, Result<List<WorkTask>>>
{
    private readonly IWorkTaskRepository _repository;
    private readonly ICacheService _cache;
    private readonly ICurrentUserService _currentUserService;

    public GetAllTasksQueryHandler(
        IWorkTaskRepository repository,
        ICacheService cache,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _cache = cache;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<WorkTask>>> Handle(GetAllTasksQuery request, CancellationToken cancellationToken)
    {
        // ── OWNERSHIP ────────────────────────────────────
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result<List<WorkTask>>.Failure("Unable to determine current user");
        // ─────────────────────────────────────────────────

        // ── PER-USER CACHE KEY ───────────────────────────
        var cacheKey = CacheKeys.AllTasksForUser(userId);
        // ─────────────────────────────────────────────────

        var cached = await _cache.GetAsync<List<WorkTask>>(cacheKey, cancellationToken);
        if (cached != null)
            return Result<List<WorkTask>>.Success(cached);

        // ── FETCH ONLY THIS USER'S TASKS ──────────────────
        var tasks = await _repository.GetAllAsync(userId, cancellationToken);
        // ─────────────────────────────────────────────────

        await _cache.SetAsync(cacheKey, tasks, TimeSpan.FromSeconds(60), cancellationToken);

        return Result<List<WorkTask>>.Success(tasks);
    }
}