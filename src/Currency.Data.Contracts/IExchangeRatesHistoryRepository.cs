using Currency.Data.Contracts.Entries;

namespace Currency.Data.Contracts;

public interface IExchangeRatesHistoryRepository
{
    Task<IEnumerable<ExchangeRateEntry>> GetRateHistoryPagedAsync(string id, int pageNumber, int pageSize, 
        CancellationToken token);
    
    Task AddRateHistory(string id, IEnumerable<ExchangeRateEntry> rates, CancellationToken token);
}