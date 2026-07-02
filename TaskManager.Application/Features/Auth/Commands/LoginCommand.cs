using MediatR;
using TaskManager.Domain.Common;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Features.Auth.Commands;

// ── CHANGED ─────────────────────────────────────────────
// Now returns AuthResult (access + refresh token) instead of just a string
public record LoginCommand(
    string Email,
    string Password
) : IRequest<Result<AuthResult>>;
// ─────────────────────────────────────────────────────

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResult>>
{
    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result<AuthResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return await _authService.LoginAsync(request.Email, request.Password, cancellationToken);
    }
}