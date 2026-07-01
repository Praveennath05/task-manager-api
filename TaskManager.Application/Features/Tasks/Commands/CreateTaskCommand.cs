using MediatR;
using TaskManager.Domain.Common;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Features.Tasks.Commands;

public record CreateTaskCommand(
    string Title,
    string Description,
    DateTime? DueDate
) : IRequest<Result<WorkTask>>;

public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, Result<WorkTask>>
{
    private readonly IWorkTaskRepository _repository;
    private readonly ICacheService _cache;

    // Must match exactly — this is what gets deleted below
    private const string CacheKey = "tasks:all";
    // ─────────────────────────────────────────────────

    public CreateTaskCommandHandler(IWorkTaskRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<Result<WorkTask>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = new WorkTask
        {
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate
        };

        var created = await _repository.CreateAsync(task, cancellationToken);

        // A new task was added — the old cached list is now stale
        // Delete it so the NEXT GetAllTasksQuery fetches fresh data
        // We don't update the cache here — simpler and safer
        // to just remove it and let the next read rebuild it
        await _cache.RemoveAsync(CacheKey, cancellationToken);
        // ─────────────────────────────────────────────

        return Result<WorkTask>.Success(created);
    }
}