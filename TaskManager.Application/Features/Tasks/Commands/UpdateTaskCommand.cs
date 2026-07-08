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
    private readonly ICurrentUserService _currentUserService;

    private const string CacheKey = CacheKeys.AllTasks;

    public UpdateTaskCommandHandler(
        IWorkTaskRepository repository,
        ICacheService cache,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _cache = cache;
        _currentUserService = currentUserService;
    }

    public async Task<Result<WorkTask>> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        // ── OWNERSHIP ────────────────────────────────────
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result<WorkTask>.Failure("Unable to determine current user");
        // ─────────────────────────────────────────────────

        var existing = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (existing == null)
            return Result<WorkTask>.Failure("Task not found");

        // ── OWNERSHIP CHECK ──────────────────────────────
        // Same "Task not found" message whether it truly doesn't
        // exist OR belongs to someone else — no information leak
        if (existing.UserId != userId)
            return Result<WorkTask>.Failure("Task not found");
        // ─────────────────────────────────────────────────

        existing.Title = request.Title;
        existing.Description = request.Description;
        existing.IsCompleted = request.IsCompleted;
        existing.DueDate = request.DueDate;
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(existing, cancellationToken);

        await _cache.RemoveAsync(CacheKey, cancellationToken);
        await _cache.RemoveAsync(CacheKeys.TaskById(request.Id), cancellationToken);

        return Result<WorkTask>.Success(updated);
    }
}