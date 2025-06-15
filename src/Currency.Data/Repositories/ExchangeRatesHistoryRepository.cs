using Currency.Data.Contracts;
using Currency.Data.Contracts.Entries;
using Currency.Data.Contracts.Exceptions;
using Currency.Data.Locks;
using Currency.Data.Settings;
using Currency.Infrastructure.Contracts.Databases.Base;
using Currency.Infrastructure.Contracts.Databases.Redis;
using Currency.Infrastructure.Contracts.Databases.Redis.Entries;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Currency.Data.Repositories;

internal class ExchangeRatesHistoryRepository(
    ILogger<ExchangeRatesHistoryRepository> logger,
    DataSettings settings,
    IRedisContext context): IExchangeRatesHistoryRepository
{
    private static string Prefix => EntityPrefix.RatesHistoryPrefix;

    public async Task<IEnumerable<ExchangeRateEntry>> GetRateHistoryPagedAsync(string id, int pageNumber, int pageSize, 
        CancellationToken token)
    {
        if (token.IsCancellationRequested) return [];

        var key = $"{Prefix}:{id}";
        var start = (pageNumber - 1) * pageSize;
        var stop = start + pageSize - 1;

        var rawValues = await context.SortedSetRangeByRankAsync(key, start, stop, ascending: true);
        if (rawValues.Length == 0) return [];

        var result = rawValues
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(json => JsonConvert.DeserializeObject<ExchangeRateEntry>(json)!)
            .ToList();

        return result;
    }
    
    public async Task AddRateHistory(string id, IEnumerable<ExchangeRateEntry> rates, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;
        
        var key = $"{Prefix}:{id}";
        
        try
        {
            await using var @lock = new DataLock((IRedisLockContext)context);
            await @lock.AcquireLockAsync(key);
            
            if (await context.KeyExistsAsync(key))
                ConcurrencyException.ThrowIfExists("Exchange Rates History already exists", key);

            var entries = rates.Select(entry =>
            {
                var value = JsonConvert.SerializeObject(entry);
                var score = new DateTimeOffset(entry.Date).ToUnixTimeMilliseconds();
                return new RedisSortedSetEntry(value, score);
            });

            await context.SortedSetAddAsync(key, entries, settings.ExchangeRatesHistoryTtl);
        }
        catch (ConcurrencyException ex)
        {
            logger.LogError(ex, "Pessimistic Concurrency: {message}, Key: {key}", ex.Message, ex.LockId);
        }
    }
}