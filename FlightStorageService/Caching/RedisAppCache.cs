using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FlightStorageService.Caching;

public sealed class RedisOptions
{
    public string? Connection { get; set; }
    public string? InstanceName { get; set; }
}

public sealed class RedisAppCache : IAppCache
{
    private readonly IDistributedCache _dist;
    private readonly IConnectionMultiplexer _mux;
    private readonly string _prefix;

    public RedisAppCache(IDistributedCache dist, IConnectionMultiplexer mux, IOptions<RedisOptions> opt)
    {
        _dist = dist;
        _mux = mux;
        _prefix = opt.Value.InstanceName ?? "fis:";
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await _dist.GetAsync(_prefix + key, ct);
        return bytes is null ? default : JsonSerializer.Deserialize<T>(bytes);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        await _dist.SetAsync(_prefix + key, bytes,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
        => _dist.RemoveAsync(_prefix + key, ct);

    public async Task ClearAllAsync(CancellationToken ct = default)
    {
        var db = _mux.GetDatabase();
        foreach (var ep in _mux.GetEndPoints())
        {
            var server = _mux.GetServer(ep);
            if (!server.IsConnected) continue;

            var batch = new List<RedisKey>(512);
            foreach (var key in server.Keys(pattern: _prefix + "*"))
            {
                batch.Add(key);
                if (batch.Count >= 512)
                {
                    await db.KeyDeleteAsync(batch.ToArray());
                    batch.Clear();
                }
                if (ct.IsCancellationRequested) break;
            }
            if (batch.Count > 0)
                await db.KeyDeleteAsync(batch.ToArray());
        }
    }
}
