using FlightStorageService.Caching;
using Microsoft.Extensions.Caching.Memory;

public sealed class InMemoryAppCache : IAppCache
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly string _pfx = "fis:";

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        => Task.FromResult(_cache.TryGetValue(_pfx + key, out var v) ? (T?)v : default);

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        _cache.Set(_pfx + key, value!, ttl);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _cache.Remove(_pfx + key);
        return Task.CompletedTask;
    }

    public Task ClearAllAsync(CancellationToken ct = default)
    {
        _cache.Compact(1.0);
        return Task.CompletedTask;
    }
}
