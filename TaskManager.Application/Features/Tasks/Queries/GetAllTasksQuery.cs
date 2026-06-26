using MediatR;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Features.Tasks.Queries;

public record GetAllTasksQuery() : IRequest<Result<List<WorkTask>>>;

public class GetAllTasksQueryHandler : IRequestHandler<GetAllTasksQuery, Result<List<WorkTask>>>
{
    private readonly IWorkTaskRepository _repository;

    public GetAllTasksQueryHandler(IWorkTaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<WorkTask>>> Handle(GetAllTasksQuery request, CancellationToken cancellationToken)
    {
        var tasks = await _repository.GetAllAsync(cancellationToken);
        return Result<List<WorkTask>>.Success(tasks);
    }
}