using MediatR;
using TaskManager.Domain.Common;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Features.Auth.Commands;

// ── COMMAND ───────────────────────────────────────────
// Carries the OLD refresh token from the client
// Returns a NEW AuthResult (new access + new refresh token)
public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthResult>>;
// ─────────────────────────────────────────────────────

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResult>>
{
    private readonly IAuthService _authService;

    public RefreshTokenCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result<AuthResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        return await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
    }
}