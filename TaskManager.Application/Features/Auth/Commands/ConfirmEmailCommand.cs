using MediatR;
using TaskManager.Domain.Common;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Features.Auth.Commands;

public record ConfirmEmailCommand(string UserId, string Token) : IRequest<Result<string>>;

public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, Result<string>>
{
    private readonly IAuthService _authService;

    public ConfirmEmailCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result<string>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        return await _authService.ConfirmEmailAsync(request.UserId, request.Token, cancellationToken);
    }
}