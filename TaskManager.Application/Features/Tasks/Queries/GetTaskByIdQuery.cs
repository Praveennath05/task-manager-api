using MediatR;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Features.Tasks.Queries;

public record GetTaskByIdQuery(int Id) : IRequest<Result<WorkTask>>;

public class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, Result<WorkTask>>
{
    private readonly IWorkTaskRepository _repository;

    public GetTaskByIdQueryHandler(IWorkTaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<WorkTask>> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(request.Id,cancellationToken);
        if (task == null)
            return Result<WorkTask>.Failure("Task not found");

        return Result<WorkTask>.Success(task);
    }
}