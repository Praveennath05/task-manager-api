using MediatR;
using TaskManager .Domain.Common;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Features.Auth.Commands;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<Result<string>>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<string>>
{
    
    private readonly IAuthService _authService;
    public LoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result<string>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return await _authService.LoginAsync(request.Email, request.Password, cancellationToken);
    }
}


