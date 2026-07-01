namespace TaskManager.Domain.Interfaces;

// ── INTERFACE ─────────────────────────────────────────
// Application layer only knows THIS contract
// It has no idea Redis exists — could be Redis, in-memory, anything
// Generic <T> so it works for WorkTask, List<WorkTask>, or any type
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken);
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken);
    Task RemoveAsync(string key, CancellationToken cancellationToken);
}
// ─────────────────────────────────────────────────────