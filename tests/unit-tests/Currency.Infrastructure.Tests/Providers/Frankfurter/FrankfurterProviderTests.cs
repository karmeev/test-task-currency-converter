using Currency.Common.Providers;
using Currency.Infrastructure.Contracts.Integrations.Providers.Frankfurter;
using Currency.Infrastructure.Integrations.Providers.Frankfurter;
using Currency.Infrastructure.Integrations.Providers.Frankfurter.Responses;
using Moq;

namespace Currency.Infrastructure.Tests.Providers.Frankfurter;

[TestFixture]
[Category("Unit")]
public class FrankfurterProviderTests
{
    private Mock<IFrankfurterClient> _clientMock = null!;
    private FrankfurterProvider _sut = null!;

    [SetUp]
    public void Setup()
    {
        _clientMock = new Mock<IFrankfurterClient>();
        _sut = new FrankfurterProvider(_clientMock.Object);
    }

    [Test]
    public async Task GetLatestAsync_MapsResponseToExchangeRates()
    {
        var baseCurrency = "USD";
        var apiResponse = new GetLatestExchangeRateResponse
        {
            Base = baseCurrency,
            Date = new DateOnly(2023, 6, 15),
            Rates = new Dictionary<string, decimal> { ["EUR"] = 0.95m }
        };

        _clientMock.Setup(c => c.GetLatestExchangeRateAsync(baseCurrency, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        var request = new GetLatestRequest(baseCurrency);

        var result = await _sut.GetLatestAsync(request);

        Assert.That(result.Provider, Is.EqualTo(ProvidersConst.Frankfurter));
        Assert.That(result.CurrentCurrency, Is.EqualTo(baseCurrency));
        Assert.That(result.LastDate, Is.EqualTo(apiResponse.Date.ToDateTime(TimeOnly.MinValue)));
        Assert.That(result.Rates, Is.EqualTo(apiResponse.Rates));
    }

    [Test]
    public async Task GetLatestForCurrenciesAsync_MapsResponseToExchangeRates()
    {
        var baseCurrency = "USD";
        var symbols = new[] { "EUR", "GBP" };
        var apiResponse = new GetLatestExchangeRatesResponse
        {
            Base = baseCurrency,
            Date = new DateOnly(2023, 6, 15),
            Rates = new Dictionary<string, decimal> { ["EUR"] = 0.95m, ["GBP"] = 0.82m }
        };

        _clientMock.Setup(c => c.GetLatestExchangeRatesAsync(baseCurrency, symbols, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        var request = new GetLatestForCurrenciesRequest(baseCurrency, symbols);

        var result = await _sut.GetLatestForCurrenciesAsync(request);

        Assert.That(result.Provider, Is.EqualTo(ProvidersConst.Frankfurter));
        Assert.That(result.CurrentCurrency, Is.EqualTo(baseCurrency));
        Assert.That(result.LastDate, Is.EqualTo(apiResponse.Date.ToDateTime(TimeOnly.MinValue)));
        Assert.That(result.Rates, Is.EqualTo(apiResponse.Rates));
    }

    [Test]
    public async Task GetHistoryAsync_MapsResponseToExchangeRatesHistory()
    {
        var baseCurrency = "USD";
        var start = new DateOnly(2023, 6, 10);
        var end = new DateOnly(2023, 6, 15);

        var apiResponse = new GetExchangeRatesHistoryResponse
        {
            Base = baseCurrency,
            StartDate = start,
            EndDate = end,
            Rates = new Dictionary<DateOnly, Dictionary<string, decimal>>
            {
                [new DateOnly(2023, 6, 10)] = new Dictionary<string, decimal> { ["EUR"] = 0.95m },
                [new DateOnly(2023, 6, 11)] = new Dictionary<string, decimal> { ["EUR"] = 0.96m }
            }
        };

        _clientMock.Setup(c => c.GetExchangeRatesHistoryAsync(baseCurrency, start, end, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        var request = new GetHistoryRequest(baseCurrency, start, end);

        var result = await _sut.GetHistoryAsync(request);

        Assert.That(result.Provider, Is.EqualTo(ProvidersConst.Frankfurter));
        Assert.That(result.CurrentCurrency, Is.EqualTo(baseCurrency));
        Assert.That(result.StartDate, Is.EqualTo(start.ToDateTime(TimeOnly.MinValue)));
        Assert.That(result.EndDate, Is.EqualTo(end.ToDateTime(TimeOnly.MinValue)));

        // Rates keys should be converted DateOnly -> DateTime and values preserved
        Assert.That(result.Rates.Keys, Has.All.Matches<DateTime>(dt => dt >= start.ToDateTime(TimeOnly.MinValue) && dt <= end.ToDateTime(TimeOnly.MinValue)));

        // Verify rate values per date
        var expectedRates = apiResponse.Rates.ToDictionary(
            kvp => kvp.Key.ToDateTime(TimeOnly.MinValue),
            kvp => kvp.Value);

        Assert.That(result.Rates, Is.EquivalentTo(expectedRates));
    }

    [Test]
    public void Dispose_DisposesClient()
    {
        var clientMock = new Mock<IFrankfurterClient>();
        var provider = new FrankfurterProvider(clientMock.Object);

        provider.Dispose();

        clientMock.Verify(c => c.Dispose(), Times.Once);
    }

    [TearDown]
    public void TearDown()
    {
        _sut.Dispose();
    }
}
