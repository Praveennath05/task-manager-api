namespace TaskManager.Domain.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
}