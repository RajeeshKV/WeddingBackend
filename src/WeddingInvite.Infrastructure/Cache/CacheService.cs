using Microsoft.Extensions.Caching.Memory;
using WeddingInvite.Application.Interfaces;

namespace WeddingInvite.Infrastructure.Cache;

public sealed class CacheService(IMemoryCache memoryCache) : ICacheService
{
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly HashSet<string> _keys = new();
    private readonly object _lock = new();

    public string GetWeddingPublicConfigKey(Guid weddingId) => TrackKey($"wedding:{weddingId}:public-config");
    public string GetWeddingGalleryKey(Guid weddingId, string queryHash) => TrackKey($"wedding:{weddingId}:gallery:{queryHash}");
    public string GetWeddingEventsKey(Guid weddingId) => TrackKey($"wedding:{weddingId}:events");
    public string GetWeddingSettingsKey(Guid weddingId) => TrackKey($"wedding:{weddingId}:settings");
    public string GetAdminDashboardKey(Guid weddingId, string queryHash) => TrackKey($"wedding:{weddingId}:admin-dashboard:{queryHash}");

    private string TrackKey(string key)
    {
        lock (_lock)
        {
            _keys.Add(key);
        }
        return key;
    }

    public void Remove(string key)
    {
        lock (_lock)
        {
            _memoryCache.Remove(key);
            _keys.Remove(key);
        }
    }

    public void RemoveByPrefix(string prefix)
    {
        lock (_lock)
        {
            var toRemove = _keys.Where(k => k.StartsWith(prefix)).ToList();
            foreach (var key in toRemove)
            {
                _memoryCache.Remove(key);
                _keys.Remove(key);
            }
        }
    }

    public void RemoveWedding(string weddingId)
    {
        RemoveByPrefix($"wedding:{weddingId}:");
    }
}
