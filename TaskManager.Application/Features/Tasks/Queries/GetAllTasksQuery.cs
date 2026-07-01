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

    // ── CACHE KEY ──────────────────────────────────────
    // A constant so it's spelled the same everywhere we reference it
    // Both here (read) and in Create/Update/Delete (invalidate) later
    private const string CacheKey = "tasks:all";
    // ─────────────────────────────────────────────────

    public GetAllTasksQueryHandler(IWorkTaskRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<Result<List<WorkTask>>> Handle(GetAllTasksQuery request, CancellationToken cancellationToken)
    {
        // ── CHECK CACHE FIRST ──────────────────────────
        // If tasks are already in Redis, skip the database entirely
        var cached = await _cache.GetAsync<List<WorkTask>>(CacheKey, cancellationToken);
        if (cached != null)
            return Result<List<WorkTask>>.Success(cached);
        // ─────────────────────────────────────────────

        // ── CACHE MISS — GO TO DATABASE ────────────────
        var tasks = await _repository.GetAllAsync(cancellationToken);

        // ── STORE IN CACHE FOR NEXT TIME ───────────────
        // Expires in 60 seconds — after that, next request
        // hits the database again and refreshes the cache
        await _cache.SetAsync(CacheKey, tasks, TimeSpan.FromSeconds(60), cancellationToken);
        // ─────────────────────────────────────────────

        return Result<List<WorkTask>>.Success(tasks);
    }
}