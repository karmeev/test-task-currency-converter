using System.Text.Json;
using Currency.Infrastructure.Contracts.Databases.Base;
using Currency.Infrastructure.Contracts.Databases.Redis;
using Currency.Infrastructure.Contracts.Databases.Redis.Entries;
using Currency.Infrastructure.Settings;
using StackExchange.Redis;

namespace Currency.Infrastructure.Redis;

internal class RedisContext(
    InfrastructureSettings settings,
    IConnectionMultiplexer connection) : IRedisContext, IRedisLockContext
{
    public int RetryCount => settings.RedisSettings.DataLockRetryCount;
    public int RetryDelayMilliseconds => settings.RedisSettings.DataLockRetryDelayMilliseconds;
    private RedisSettings Settings => settings.RedisSettings;
    
    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
    {
        var serialized = JsonSerializer.Serialize(value);
        var db = GetDatabase(key);
        await db.StringSetAsync(key, serialized, ttl);
    }

    public async Task<T> TryGetAsync<T>(string key)
    {
        var db = GetDatabase(key);
        var value = await db.StringGetAsync(key);
        return value.HasValue ? JsonSerializer.Deserialize<T>(value!) : default;
    }
    
    public async Task<T> TryGetByIndexAsync<T>(string index)
    {
        var db = GetIndexesDatabase();
        var value = await db.StringGetAsync(index);
        var key = value.ToString();
        db = GetDatabase(index);
        var entity = await db.StringGetAsync(key);
        if (!entity.HasValue) return default;
        return JsonSerializer.Deserialize<T>(entity!);
    }
    
    public Task<bool> KeyExistsAsync(string key)
    {
        var db = GetDatabase(key);
        return db.KeyExistsAsync(key);
    }
    
    public async Task SortedSetAddAsync(string key, IEnumerable<RedisSortedSetEntry> entries, TimeSpan? ttl = null)
    {
        var db = GetDatabase(key);

        var redisEntries = entries
            .Select(e => new SortedSetEntry(e.Value, e.Score))
            .ToArray();

        await db.SortedSetAddAsync(key, redisEntries);
        
        if (ttl.HasValue)
            await db.KeyExpireAsync(key, ttl);
    }
    
    public async Task<string[]> SortedSetRangeByRankAsync(string key, long start, long stop, bool ascending = true)
    {
        var db = GetDatabase(key);
        var order = ascending ? Order.Ascending : Order.Descending;

        var redisValues = await db.SortedSetRangeByRankAsync(key, start, stop, order);

        return redisValues.Select(rv => rv.ToString()).ToArray();
    }
    
    public async Task<bool> AcquireLockAsync(string key, string lockId)
    {
        var ttl = new TimeSpan(0,0,0,0, Settings.DataLockMilliseconds);
        var db = GetLocksDatabase();
        return await db.StringSetAsync($"lock:{key}", lockId, ttl, when: When.NotExists);
    }

    public async Task<bool> ReleaseLockAsync(string key, string lockId)
    {
        var db = GetLocksDatabase();
        
        var script = """
                             if redis.call('get', KEYS[1]) == ARGV[1] then
                                 return redis.call('del', KEYS[1])
                             else
                                 return 0
                             end
                     """;

        var result = (int)await db.ScriptEvaluateAsync(
            script,
            keys: [$"lock:{key}"],
            values: [lockId]);

        return result == 1;
    }

    private IDatabase GetIndexesDatabase() => connection.GetDatabase(0);
    private IDatabase GetLocksDatabase() => connection.GetDatabase(1);

    private IDatabase GetDatabase(string key)
    {
        if (key.StartsWith(EntityPrefix.AuthPrefix)) 
            return connection.GetDatabase(Settings.RefreshTokensDatabaseNumber);
        
        if (key.StartsWith(EntityPrefix.RatesHistoryPrefix)) 
            return connection.GetDatabase(Settings.ExchangeRatesHistoryDatabaseNumber);
        
        if (key.StartsWith(EntityPrefix.ExchangeRatesPrefix)) 
            return connection.GetDatabase(Settings.ExchangeRatesDatabaseNumber);
        
        if (key.StartsWith(EntityPrefix.UserPrefix)) 
            return connection.GetDatabase(15);

        return connection.GetDatabase(14);
    }
}