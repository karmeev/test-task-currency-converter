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
    private Mock<IRedisContext> _mockContext;
    private Mock<ILogger<ExchangeRatesRepository>> _mockLogger;
    private IExchangeRatesRepository _sut;
    private DataSettings _settings;

    [SetUp]
    public void Setup()
    {
        _mockContext = new Mock<IRedisContext>();
        _mockLogger = new Mock<ILogger<ExchangeRatesRepository>>();
        _settings = new DataSettings(new Mock<IOptionsMonitor<CacheSettings>>().Object);
        _sut = new ExchangeRatesRepository(_mockLogger.Object, _settings, _mockContext.Object);
    }

    [Test]
    public async Task GetExchangeRates_ShouldReturnRates()
    {
        // Arrange
        var expected = new ExchangeRates();
        _mockContext.Setup(x => x.TryGetAsync<ExchangeRates>("exchange_rates:eur")).ReturnsAsync(expected);

        // Act
        var result = await _sut.GetExchangeRates("eur", CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetCurrencyConversionAsync_ShouldReturnConversion()
    {
        // Arrange
        var conversion = new CurrencyConversion();
        _mockContext.Setup(x => x.TryGetAsync<CurrencyConversion>("exchange_rates:conv"))
            .ReturnsAsync(conversion);

        // Act
        var result = await _sut.GetCurrencyConversionAsync("conv", CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(conversion));
    }
}
