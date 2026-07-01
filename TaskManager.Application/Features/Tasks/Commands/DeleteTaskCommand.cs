using MediatR;
using TaskManager.Domain.Common;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Features.Tasks.Commands;

public record DeleteTaskCommand(int Id) : IRequest<Result<bool>>;

public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, Result<bool>>
{
    private readonly IWorkTaskRepository _repository;
    private readonly ICacheService _cache;

    private const string CacheKey = "tasks:all";

    public DeleteTaskCommandHandler(IWorkTaskRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<Result<bool>> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (existing == null)
            return Result<bool>.Failure("Task not found");

        await _repository.DeleteAsync(request.Id, cancellationToken);

        // ── CACHE INVALIDATION ─────────────────────────
        // Task deleted — old cached list still contains it
        await _cache.RemoveAsync(CacheKey, cancellationToken);
        await _cache.RemoveAsync($"tasks:{request.Id}", cancellationToken);
        // ─────────────────────────────────────────────

        return Result<bool>.Success(true);
    }
}