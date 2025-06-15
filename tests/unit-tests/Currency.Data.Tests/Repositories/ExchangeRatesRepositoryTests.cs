using Currency.Data.Contracts;
using Currency.Data.Repositories;
using Currency.Data.Settings;
using Currency.Domain.Operations;
using Currency.Domain.Rates;
using Currency.Infrastructure.Contracts.Databases.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Currency.Data.Tests.Repositories;

[Category("Unit")]
public class ExchangeRatesRepositoryTests
{
    private Mock<IRedisLockContext> _mockContext;
    private Mock<ILogger<ExchangeRatesRepository>> _mockLogger;
    private DataSettings _settings;
    private IExchangeRatesRepository _sut;

    [SetUp]
    public void Setup()
    {
        _mockContext = new Mock<IRedisLockContext>();
        _mockLogger = new Mock<ILogger<ExchangeRatesRepository>>();
        _settings = new DataSettings(new Mock<IOptionsMonitor<CacheSettings>>().Object);
        _sut = new ExchangeRatesRepository(_mockLogger.Object, _settings, _mockContext.Object);
    }

    [Test]
    public async Task GetExchangeRates_ShouldReturnRates()
    {
        var expected = new ExchangeRates();
        _mockContext.Setup(x => x.TryGetAsync<ExchangeRates>("exchange_rates:id")).ReturnsAsync(expected);

        var result = await _sut.GetExchangeRates("id", CancellationToken.None);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void GetExchangeRates_CancelledToken_Throws()
    {
        var ct = new CancellationToken(true);
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _sut.GetExchangeRates("id", ct));
    }

    [Test]
    public async Task GetCurrencyConversionAsync_ShouldReturn()
    {
        var expected = new CurrencyConversion();
        _mockContext.Setup(x => x.TryGetAsync<CurrencyConversion>("exchange_rates:id"))
            .ReturnsAsync(expected);

        var result = await _sut.GetCurrencyConversionAsync("id", CancellationToken.None);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void GetCurrencyConversionAsync_CancelledToken_Throws()
    {
        var ct = new CancellationToken(true);
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _sut.GetCurrencyConversionAsync("id", ct));
    }
    
    [Test]
    public async Task AddExchangeRates_ShouldCompleteWithoutException()
    {
        var exchangeRates = new ExchangeRates();
        var token = CancellationToken.None;

        Assert.DoesNotThrowAsync(async () =>
            await _sut.AddExchangeRates("test-id", exchangeRates, token));
    }

    [Test]
    public async Task AddConversionResultAsync_ShouldCompleteWithoutException()
    {
        var conversion = new CurrencyConversion();
        var token = CancellationToken.None;

        Assert.DoesNotThrowAsync(async () =>
            await _sut.AddConversionResultAsync("test-id", conversion, token));
    }
}

