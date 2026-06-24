using MediatR;
using TaskManager.Application.Common;
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

    public UpdateTaskCommandHandler(IWorkTaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<WorkTask>> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.Id);
        if (existing == null)
            return Result<WorkTask>.Failure("Task not found");

        existing.Title = request.Title;
        existing.Description = request.Description;
        existing.IsCompleted = request.IsCompleted;
        existing.DueDate = request.DueDate;
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(existing);
        return Result<WorkTask>.Success(updated);
    }
}