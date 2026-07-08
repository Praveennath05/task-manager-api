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
    private readonly ICurrentUserService _currentUserService;

    private const string CacheKey = CacheKeys.AllTasks;

    public CreateTaskCommandHandler(
        IWorkTaskRepository repository,
        ICacheService cache,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _cache = cache;
        _currentUserService = currentUserService;
    }

    public async Task<Result<WorkTask>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        // ── OWNERSHIP ────────────────────────────────────
        // Stamp the task with whoever is currently logged in
        // If somehow null (shouldn't happen since [Authorize]
        // already blocks unauthenticated requests), fail clearly
        // rather than silently creating an orphaned task
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result<WorkTask>.Failure("Unable to determine current user");
        // ─────────────────────────────────────────────────

        var task = new WorkTask
        {
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            UserId = userId
        };

        var created = await _repository.CreateAsync(task, cancellationToken);

        await _cache.RemoveAsync(CacheKey, cancellationToken);

        return Result<WorkTask>.Success(created);
    }
}