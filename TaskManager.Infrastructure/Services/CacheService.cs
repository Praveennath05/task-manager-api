using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Infrastructure.Services;

public class CacheService : ICacheService
{
    // ── DEPENDENCY INJECTION ──────────────────────────────
    // IDistributedCache is Redis under the hood (we registered it earlier)
    // Only THIS class knows that — Application never sees Redis directly
    private readonly IDistributedCache _cache;

    public CacheService(IDistributedCache cache)
    {
        _cache = cache;
    }
    // ─────────────────────────────────────────────────────

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        // Redis only stores strings/bytes — we store JSON text
        var data = await _cache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrEmpty(data))
            return default; // cache miss — nothing found

        return JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken)
    {
        var data = JsonSerializer.Serialize(value);

        var options = new DistributedCacheEntryOptions
        {
            // ── EXPIRATION ─────────────────────────────
            // After this time, Redis automatically deletes the key
            // Prevents stale data from living forever
            AbsoluteExpirationRelativeToNow = expiration
        };

        await _cache.SetStringAsync(key, data, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }
}