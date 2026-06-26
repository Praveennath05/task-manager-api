using TaskManager.Domain.Common;

namespace TaskManager.Domain.Interfaces;

public interface IAuthService
{
    Task<Result<string>> RegisterAsync(string email, string password,CancellationToken cancellationToken);

    Task<Result<string>> LoginAsync(string email, string password,CancellationToken cancellationToken);
}