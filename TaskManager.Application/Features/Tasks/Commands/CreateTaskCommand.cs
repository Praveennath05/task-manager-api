using MediatR;
using TaskManager.Application.Common;
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

    public CreateTaskCommandHandler(IWorkTaskRepository repository)
    {
        _repository = repository;
    }
public async Task<Result<WorkTask>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
{
    var task = new WorkTask
    {
        Title = request.Title,
        Description = request.Description,
        DueDate = request.DueDate
    };

    var created = await _repository.CreateAsync(task);
    return Result<WorkTask>.Success(created);
}
}