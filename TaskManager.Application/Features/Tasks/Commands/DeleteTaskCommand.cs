using MediatR;
using TaskManager.Domain.Common;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Features.Tasks.Commands;

public record DeleteTaskCommand(int Id) : IRequest<Result<bool>>;

public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, Result<bool>>
{
    private readonly IWorkTaskRepository _repository;
    private readonly ICacheService _cache;
    private readonly ICurrentUserService _currentUserService;

    private const string CacheKey = CacheKeys.AllTasks;

    public DeleteTaskCommandHandler(
        IWorkTaskRepository repository,
        ICacheService cache,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _cache = cache;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        // ── OWNERSHIP ────────────────────────────────────
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result<bool>.Failure("Unable to determine current user");
        // ─────────────────────────────────────────────────

        var existing = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (existing == null)
            return Result<bool>.Failure("Task not found");

        // ── OWNERSHIP CHECK ──────────────────────────────
        if (existing.UserId != userId)
            return Result<bool>.Failure("Task not found");
        // ─────────────────────────────────────────────────

        await _repository.DeleteAsync(request.Id, cancellationToken);

        await _cache.RemoveAsync(CacheKey, cancellationToken);
        await _cache.RemoveAsync(CacheKeys.TaskById(request.Id), cancellationToken);

        return Result<bool>.Success(true);
    }
}