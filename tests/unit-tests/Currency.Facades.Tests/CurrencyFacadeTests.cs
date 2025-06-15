using Bogus;
using Currency.Data.Contracts;
using Currency.Domain.Operations;
using Currency.Domain.Rates;
using Currency.Facades.Contracts.Requests;
using Currency.Facades.Tests.Utility;
using Currency.Services.Contracts.Application;
using Currency.Services.Contracts.Domain;
using Microsoft.Extensions.Logging;
using Moq;

namespace Currency.Facades.Tests;

[TestFixture]
[Category("Unit")]
public class CurrencyFacadeTests
{
    private Faker _faker;
    private ILogger<CurrencyFacade> _logger;

    [SetUp]
    public void SetUp()
    {
        _faker = new Faker();
        _logger = Test.GetLogger<CurrencyFacade>();
    }

    [Test]
    public async Task RetrieveLatestExchangeRatesAsync_WhenRatesExistInRepository_ReturnsExistedRates()
    {
        Test.StartTest();
        
        // Arrange
        var currency = "EUR";
        var expectedRates = new ExchangeRates
        {
            CurrentCurrency = currency,
            LastDate = DateTime.UtcNow,
            Rates = new Dictionary<string, decimal> { { "USD", 1.0m } }
        };

        var repoMock = new Mock<IExchangeRatesRepository>();
        repoMock.Setup(r => r.GetExchangeRates(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRates);

        var sut = new CurrencyFacade(
            _logger,
            Mock.Of<IConverterService>(),
            Mock.Of<IExchangeRatesService>(),
            Mock.Of<IPublisherService>(),
            repoMock.Object);

        // Act
        var result = await sut.RetrieveLatestExchangeRatesAsync(currency, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.CurrentCurrency, Is.EqualTo(expectedRates.CurrentCurrency));
            Assert.That(result.LastDate, Is.EqualTo(expectedRates.LastDate));
            Assert.That(result.Rates, Is.EqualTo(expectedRates.Rates));
        });
        
        Test.CompleteTest();
    }

    [Test]
    public async Task ConvertToCurrencyAsync_WhenCurrencyExistsInRepo_ReturnsConvertedValue()
    {
        Test.StartTest();
        
        // Arrange
        var request = new ConvertToCurrencyRequest
        {
            Amount = _faker.Random.Decimal(10, 1000),
            FromCurrency = "EUR",
            ToCurrency = "USD"
        };

        var expectedConversion = new CurrencyConversion
        {
            Amount = request.Amount * 1.1m,
            ToCurrency = request.ToCurrency
        };

        var repoMock = new Mock<IExchangeRatesRepository>();
        repoMock.Setup(r => r.GetCurrencyConversionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedConversion);

        var sut = new CurrencyFacade(
            _logger,
            Mock.Of<IConverterService>(),
            Mock.Of<IExchangeRatesService>(),
            Mock.Of<IPublisherService>(),
            repoMock.Object);

        // Act
        var result = await sut.ConvertToCurrencyAsync(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Amount, Is.EqualTo(expectedConversion.Amount));
            Assert.That(result.Currency, Is.EqualTo(expectedConversion.ToCurrency));
        });
        
        Test.CompleteTest();
    }
    
    [Test]
    public async Task ConvertToCurrencyAsync_WhenCurrencyNotExistsInRepo_ReturnsConvertedValue()
    {
        Test.StartTest();
        
        // Arrange
        var request = new ConvertToCurrencyRequest
        {
            Amount = _faker.Random.Decimal(10, 1000),
            FromCurrency = "EUR",
            ToCurrency = "USD"
        };

        var expectedConversion = new CurrencyConversion
        {
            Amount = request.Amount * 1.1m,
            ToCurrency = request.ToCurrency
        };

        var repoMock = new Mock<IExchangeRatesRepository>();
        repoMock.Setup(r => r.GetCurrencyConversionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()));

        var service = new Mock<IConverterService>();
        service.Setup(r => r.ConvertToCurrency(It.IsAny<decimal>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedConversion);
        
        var sut = new CurrencyFacade(
            _logger,
            service.Object,
            Mock.Of<IExchangeRatesService>(),
            Mock.Of<IPublisherService>(),
            repoMock.Object);

        // Act
        var result = await sut.ConvertToCurrencyAsync(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Amount, Is.EqualTo(expectedConversion.Amount));
            Assert.That(result.Currency, Is.EqualTo(expectedConversion.ToCurrency));
        });
        
        Test.CompleteTest();
    }

    [Test]
    public void RetrieveLatestExchangeRatesAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        Test.StartTest();
        
        // Arrange
        var currency = "EUR";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var sut = new CurrencyFacade(
            _logger,
            Mock.Of<IConverterService>(),
            Mock.Of<IExchangeRatesService>(),
            Mock.Of<IPublisherService>(),
            Mock.Of<IExchangeRatesRepository>());

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await sut.RetrieveLatestExchangeRatesAsync(currency, cts.Token));
        
        Test.CompleteTest();
    }
}
