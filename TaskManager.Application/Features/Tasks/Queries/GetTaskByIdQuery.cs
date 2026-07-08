using MediatR;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Features.Tasks.Queries;

public record GetTaskByIdQuery(int Id) : IRequest<Result<WorkTask>>;

public class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, Result<WorkTask>>
{
    private readonly IWorkTaskRepository _repository;
    private readonly ICacheService _cache;
    private readonly ICurrentUserService _currentUserService;

    public GetTaskByIdQueryHandler(
        IWorkTaskRepository repository,
        ICacheService cache,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _cache = cache;
        _currentUserService = currentUserService;
    }

    public async Task<Result<WorkTask>> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        // ── OWNERSHIP ────────────────────────────────────
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result<WorkTask>.Failure("Unable to determine current user");
        // ─────────────────────────────────────────────────

        var cacheKey = CacheKeys.TaskById(request.Id);

        var cached = await _cache.GetAsync<WorkTask>(cacheKey, cancellationToken);
        if (cached != null)
        {
            // ── OWNERSHIP CHECK — EVEN ON A CACHE HIT ────────
            // IMPORTANT: don't skip this check just because the
            // data came from cache. If we didn't check here, User A
            // could potentially see User B's cached task, since the
            // cache itself doesn't know who's asking
            if (cached.UserId != userId)
                return Result<WorkTask>.Failure("Task not found");
            // ─────────────────────────────────────────────

            return Result<WorkTask>.Success(cached);
        }

        var task = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (task == null)
            return Result<WorkTask>.Failure("Task not found");

        // ── OWNERSHIP CHECK — ON A CACHE MISS TOO ─────────
        // Same message either way ("Task not found") — this is
        // exactly what makes it behave like 404, not 403:
        // an attacker gets the identical response whether the
        // task doesn't exist OR exists but belongs to someone else
        if (task.UserId != userId)
            return Result<WorkTask>.Failure("Task not found");
        // ─────────────────────────────────────────────────

        await _cache.SetAsync(cacheKey, task, TimeSpan.FromSeconds(60), cancellationToken);

        return Result<WorkTask>.Success(task);
    }
}