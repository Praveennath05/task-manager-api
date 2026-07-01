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

    public GetTaskByIdQueryHandler(IWorkTaskRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<Result<WorkTask>> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        // ── UNIQUE KEY PER TASK ────────────────────────────
        // Different from "tasks:all" — this caches ONE task at a time
        // e.g. "tasks:1", "tasks:2", "tasks:3" — each task has its own slot
        var cacheKey = $"tasks:{request.Id}";
        // ─────────────────────────────────────────────────

        var cached = await _cache.GetAsync<WorkTask>(cacheKey, cancellationToken);
        if (cached != null)
            return Result<WorkTask>.Success(cached);

        var task = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (task == null)
            return Result<WorkTask>.Failure("Task not found");

        await _cache.SetAsync(cacheKey, task, TimeSpan.FromSeconds(60), cancellationToken);

        return Result<WorkTask>.Success(task);
    }
}