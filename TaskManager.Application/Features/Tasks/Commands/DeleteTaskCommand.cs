using MediatR;
using TaskManager.Domain.Common;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Features.Tasks.Commands;

public record DeleteTaskCommand(int Id) : IRequest<Result<bool>>;

public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, Result<bool>>
{
    private readonly IWorkTaskRepository _repository;

    public DeleteTaskCommandHandler(IWorkTaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<bool>> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (existing == null)
            return Result<bool>.Failure("Task not found");

        await _repository.DeleteAsync(request.Id, cancellationToken);
        return Result<bool>.Success(true);
    }
}