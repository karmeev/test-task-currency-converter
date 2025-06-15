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
    private Mock<IRedisLockContext> _mockContext;
    private Mock<ILogger<ExchangeRatesHistoryRepository>> _mockLogger;
    private IExchangeRatesHistoryRepository _sut;
    private DataSettings _settings;

    [SetUp]
    public void Setup()
    {
        _mockContext = new Mock<IRedisLockContext>();
        _mockLogger = new Mock<ILogger<ExchangeRatesHistoryRepository>>();
        _settings = new DataSettings(new Mock<IOptionsMonitor<CacheSettings>>().Object);
        _sut = new ExchangeRatesHistoryRepository(_mockLogger.Object, _settings, _mockContext.Object);
    }

    [Test]
    public async Task GetRateHistoryPagedAsync_CancellationRequested_ReturnsEmpty()
    {
        var ct = new CancellationToken(true); // canceled
        var result = await _sut.GetRateHistoryPagedAsync("id", 1, 10, ct);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetRateHistoryPagedAsync_NoValues_ReturnsEmpty()
    {
        _mockContext.Setup(x => x.SortedSetRangeByRankAsync(It.IsAny<string>(), 0, 9, true))
            .ReturnsAsync(Array.Empty<string>());

        var result = await _sut.GetRateHistoryPagedAsync("id", 1, 10, CancellationToken.None);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetRateHistoryPagedAsync_ValidValues_ParsesCorrectly()
    {
        var json = JsonConvert.SerializeObject(new ExchangeRateEntry { Date = DateTime.UtcNow });
        _mockContext.Setup(x => x.SortedSetRangeByRankAsync("history:id", 0, 9, true))
            .ReturnsAsync(new[] { json });

        var result = await _sut.GetRateHistoryPagedAsync("id", 1, 10, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
    }
    
    [Test]
    public async Task AddRateHistory_ShouldCompleteWithoutException()
    {
        var entries = new List<ExchangeRateEntry>
        {
            new ExchangeRateEntry { Date = DateTime.UtcNow, Value = 1.23M }
        };

        var token = CancellationToken.None;

        Assert.DoesNotThrowAsync(async () =>
            await _sut.AddRateHistory("test-id", entries, token));
    }
}

