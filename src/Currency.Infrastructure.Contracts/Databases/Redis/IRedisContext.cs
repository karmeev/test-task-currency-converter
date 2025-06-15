using Currency.Infrastructure.Contracts.Databases.Redis.Entries;

namespace Currency.Infrastructure.Contracts.Databases.Redis;

public interface IRedisContext
{
    Task<T> TryGetAsync<T>(string key);
    Task<T> TryGetByIndexAsync<T>(string index);
    Task<bool> KeyExistsAsync(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null);
    Task SortedSetAddAsync(string key, IEnumerable<RedisSortedSetEntry> entries, TimeSpan? ttl = null);
    Task<string[]> SortedSetRangeByRankAsync(string key, long start, long stop, bool ascending = true);
}