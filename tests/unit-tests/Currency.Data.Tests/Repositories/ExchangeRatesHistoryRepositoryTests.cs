using Currency.Data.Contracts;
using Currency.Data.Contracts.Entries;
using Currency.Data.Repositories;
using Currency.Data.Settings;
using Currency.Infrastructure.Contracts.Databases.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;

namespace Currency.Data.Tests.Repositories;

[Category("Unit")]
public class ExchangeRatesHistoryRepositoryTests
{
    private Mock<IRedisContext> _mockContext;
    private Mock<ILogger<ExchangeRatesHistoryRepository>> _mockLogger;
    private IExchangeRatesHistoryRepository _sut;

    [SetUp]
    public void Setup()
    {
        _mockContext = new Mock<IRedisContext>();
        _mockLogger = new Mock<ILogger<ExchangeRatesHistoryRepository>>();
        
        var settings = new DataSettings(new Mock<IOptionsMonitor<CacheSettings>>().Object);
        _sut = new ExchangeRatesHistoryRepository(_mockLogger.Object, settings, _mockContext.Object);
    }

    [Test]
    public async Task GetRateHistoryPagedAsync_ShouldDeserializeAndReturn()
    {
        // Arrange
        var entries = new[]
        {
            JsonConvert.SerializeObject(new ExchangeRateEntry { Date = DateTime.UtcNow })
        };
        _mockContext.Setup(x => x.SortedSetRangeByRankAsync("history:usd", 0, 9, true))
            .ReturnsAsync(entries);

        // Act
        var result = await _sut.GetRateHistoryPagedAsync("usd", 1, 10, CancellationToken.None);

        // Assert
        Assert.That(result, Has.Exactly(1).Items);
    }
    
    [Test]
    public async Task GetRateHistoryPagedAsync_KeyIsNoExists_ShouldReturnEmptyList()
    {
        // Arrange
        var entries = new[]
        {
            JsonConvert.SerializeObject(new ExchangeRateEntry { Date = DateTime.UtcNow })
        };
        _mockContext.Setup(x => x.SortedSetRangeByRankAsync("incorrect-key:usd", 0, 9, true))
            .ReturnsAsync(entries);

        // Act
        var result = await _sut.GetRateHistoryPagedAsync("usd", 1, 10, CancellationToken.None);

        // Assert
        Assert.That(result, Has.Exactly(0).Items);
    }
}
