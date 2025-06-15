using Currency.Infrastructure.Contracts.Integrations.Providers.Frankfurter;
using Currency.Infrastructure.Integrations.Providers.Frankfurter;
using Currency.Infrastructure.Integrations.Providers.Frankfurter.Responses;
using Currency.Infrastructure.Tests.Utility;
using Moq;

namespace Currency.Infrastructure.Tests.Integrations.Providers.Frankfurter;

[TestFixture]
[Category("Unit")]
public class FrankfurterProviderTests
{
    private Mock<IFrankfurterClient> _client;
    
    [SetUp]
    public void Setup()
    {
        _client = new Mock<IFrankfurterClient>();
    }

    [Test]
    public async Task GetLatestAsync_HappyPath_ReturnsExchangeRates()
    {
        Test.StartTest();
        
        //Arrange
        const string currency = "USD";
        var rate = new { Currency = "EUR", Amount = 1.0042m };

        _client.Setup(x => x.GetLatestExchangeRateAsync(
                It.Is<string>(c => c == currency), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetLatestExchangeRateResponse
            {
                Base = currency,
                Date = new DateOnly(2000, 1, 1),
                Rates = new Dictionary<string, decimal>
                {
                    { "EUR", 1.0042m }
                }
            });
        
        var sut = new FrankfurterProvider(_client.Object);
        
        var request = new GetLatestRequest(currency);
        
        //Act
        var result = await sut.GetLatestAsync(request, CancellationToken.None);
        
        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.CurrentCurrency, Is.EqualTo(currency));
            Assert.That(result.LastDate, Is.LessThanOrEqualTo(DateTime.UtcNow.Date));
            Assert.That(result.Rates.Count, Is.EqualTo(1));
            Assert.That(result.Rates.First().Key, Is.EqualTo(rate.Currency));
            Assert.That(result.Rates.First().Value, Is.EqualTo(rate.Amount));
        });
        
        Test.CompleteTest();
    }
    
    [Test]
    public async Task GetLatestForCurrenciesAsync_HappyPath_ReturnsExchangeRates()
    {
        Test.StartTest();
        
        //Arrange
        const string currency = "USD";
        var rates = new Dictionary<string, decimal>
        {
            { "EUR", 1.0042m }
        };

        _client.Setup(x => x.GetLatestExchangeRatesAsync(
                It.Is<string>(c => c == currency),
                It.Is<string[]>(input => !input.Contains(currency)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetLatestExchangeRatesResponse
            {
                Base = currency,
                Date = new DateOnly(2000, 1, 1),
                Rates = rates
            });
        
        var sut = new FrankfurterProvider(_client.Object);
        
        var request = new GetLatestForCurrenciesRequest(currency, ["EUR"]);
        
        //Act
        var result = await sut.GetLatestForCurrenciesAsync(request, CancellationToken.None);
        
        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.CurrentCurrency, Is.EqualTo(currency));
            Assert.That(result.LastDate, Is.LessThanOrEqualTo(DateTime.UtcNow.Date));
            Assert.That(result.Rates.Count, Is.EqualTo(1));
            Assert.That(result.Rates.First().Key, Is.EqualTo("EUR"));
            Assert.That(result.Rates.First().Value, Is.EqualTo(1.0042m));
        });
        
        Test.CompleteTest();
    }
    
    [Test]
    public async Task GetHistoryAsync_HappyPath_ReturnsExchangeRatesHistory()
    {
        Test.StartTest();
        
        //Arrange
        const string currency = "USD";
        var startDate = DateTime.UtcNow.Date.AddDays(-1);

        var rates = new Dictionary<DateOnly, Dictionary<string, decimal>>();
        rates.Add(DateOnly.FromDateTime(startDate), new Dictionary<string, decimal> { ["EUR"] = 1.0042m });

        _client.Setup(x => x.GetExchangeRatesHistoryAsync(
                It.Is<string>(c => c == currency),
                It.Is<DateOnly>(d => DateOnly.FromDateTime(startDate) == d),
                It.Is<DateOnly>(d => d > DateOnly.FromDateTime(startDate)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetExchangeRatesHistoryResponse
            {
                Amount = 1,
                Base = currency,
                StartDate = new DateOnly(startDate.Year, startDate.Month, startDate.Day),
                EndDate = new DateOnly(startDate.Year, startDate.Month, startDate.Day - 1),
                Rates = rates
            });
        
        var sut = new FrankfurterProvider(_client.Object);
        
        var request = new GetHistoryRequest(currency, DateOnly.FromDateTime(startDate), 
            DateOnly.FromDateTime(DateTime.UtcNow.Date));
        
        //Act
        var result = await sut.GetHistoryAsync(request, CancellationToken.None);
        
        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.CurrentCurrency, Is.EqualTo(currency));
            Assert.That(result.StartDate, Is.EqualTo(startDate));
            Assert.That(result.EndDate, Is.EqualTo(startDate.AddDays(-1)));
        });
        
        Test.CompleteTest();
    }
}