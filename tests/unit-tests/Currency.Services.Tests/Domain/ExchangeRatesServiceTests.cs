using Currency.Common.Providers;
using Currency.Data.Contracts;
using Currency.Data.Contracts.Entries;
using Currency.Domain.Rates;
using Currency.Infrastructure.Contracts.Integrations.Providers;
using Currency.Infrastructure.Contracts.Integrations.Providers.Base;
using Currency.Services.Domain;
using Moq;
using Frankfurter = Currency.Infrastructure.Contracts.Integrations.Providers.Frankfurter;

namespace Currency.Services.Tests.Domain;

[Category("Unit")]
public class ExchangeRatesServiceTests
{
    private Mock<ICurrencyProvidersFactory> _mockFactory;
    private Mock<IExchangeRatesHistoryRepository> _mockHistoryRepo;
    private ExchangeRatesService _sut;

    [SetUp]
    public void Setup()
    {
        _mockFactory = new Mock<ICurrencyProvidersFactory>();
        _mockHistoryRepo = new Mock<IExchangeRatesHistoryRepository>();
        _sut = new ExchangeRatesService(_mockFactory.Object, _mockHistoryRepo.Object);
    }

    [Test]
    public async Task GetLatestExchangeRates_ShouldReturnRatesFromProvider()
    {
        // Arrange
        var currency = "USD";
        var request = new Frankfurter.GetLatestRequest(currency);
        var mockProvider = new Mock<ICurrencyProvider>();
        var expectedRates = new ExchangeRates();

        _mockFactory.Setup(f => f.GetCurrencyProvider(It.IsAny<Frankfurter.GetLatestRequest>()))
            .Returns(mockProvider.Object);

        mockProvider.Setup(p => p.GetLatestAsync(It.IsAny<Frankfurter.GetLatestRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRates);

        // Act
        var result = await _sut.GetLatestExchangeRates(currency);

        // Assert
        Assert.That(result, Is.EqualTo(expectedRates));
    }

    [Test]
    public async Task GetExchangeRatesHistory_ShouldReturnHistoryFromProvider()
    {
        // Arrange
        var currency = "EUR";
        var start = new DateTime(2025, 1, 1);
        var end = new DateTime(2025, 1, 10);
        var mockProvider = new Mock<ICurrencyProvider>();
        var expectedHistory = new ExchangeRatesHistory();

        _mockFactory.Setup(f => f.GetCurrencyProvider(It.IsAny<Frankfurter.GetHistoryRequest>()))
            .Returns(mockProvider.Object);

        mockProvider.Setup(p => p.GetHistoryAsync(It.IsAny<Frankfurter.GetHistoryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedHistory);

        // Act
        var result = await _sut.GetExchangeRatesHistory(currency, start, end);

        // Assert
        Assert.That(result, Is.EqualTo(expectedHistory));
    }

    [Test]
    public async Task GetExistedRatesHistory_ShouldReturnFilteredEntries()
    {
        // Arrange
        var currency = "GBP";
        var start = new DateTime(2025, 1, 1);
        var end = new DateTime(2025, 1, 10);
        var page = 1;
        var size = 10;
        var key = $"{ProvidersConst.Frankfurter}:{currency}:{start:yyyyMMddHHmmss}:{end:yyyyMMddHHmmss}";

        var entries = new List<ExchangeRateEntry>
        {
            new() { Date = start.AddDays(-1) },  // outside range
            new() { Date = start.AddDays(1) },   // inside range
            new() { Date = end.AddDays(1) }      // outside range
        };

        _mockHistoryRepo.Setup(r => r.GetRateHistoryPagedAsync(key, page, size, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        // Act
        var result = await _sut.GetExistedRatesHistory(currency, start, end, page, size);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.First().Date, Is.InRange(start, end));
    }
}
