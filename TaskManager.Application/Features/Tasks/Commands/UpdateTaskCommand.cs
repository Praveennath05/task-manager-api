using MediatR;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Features.Tasks.Commands;

public record UpdateTaskCommand(
    int Id,
    string Title,
    string Description,
    bool IsCompleted,
    DateTime? DueDate
) : IRequest<Result<WorkTask>>;

public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, Result<WorkTask>>
{
    private readonly IWorkTaskRepository _repository;
    private readonly ICacheService _cache;

    private const string CacheKey = "tasks:all";

    public UpdateTaskCommandHandler(IWorkTaskRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<Result<WorkTask>> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (existing == null)
            return Result<WorkTask>.Failure("Task not found");

        existing.Title = request.Title;
        existing.Description = request.Description;
        existing.IsCompleted = request.IsCompleted;
        existing.DueDate = request.DueDate;
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(existing, cancellationToken);

        // ── CACHE INVALIDATION ─────────────────────────
        // Task changed — old cached list no longer reflects reality
        await _cache.RemoveAsync(CacheKey, cancellationToken);
        await _cache.RemoveAsync($"tasks:{request.Id}", cancellationToken);
        // ─────────────────────────────────────────────

        return Result<WorkTask>.Success(updated);
    }
}