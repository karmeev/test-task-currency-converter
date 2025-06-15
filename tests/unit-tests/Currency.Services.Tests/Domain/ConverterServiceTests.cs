using Currency.Domain.Rates;
using Currency.Infrastructure.Contracts.Integrations.Providers;
using Currency.Infrastructure.Contracts.Integrations.Providers.Base;
using Currency.Services.Contracts.Application.Exceptions;
using Currency.Services.Domain;
using Moq;
using Frankfurter = Currency.Infrastructure.Contracts.Integrations.Providers.Frankfurter;

namespace Currency.Services.Tests.Domain;

[Category("Unit")]
public class ConverterServiceTests
{
    private Mock<ICurrencyProvidersFactory> _mockFactory;
    private ConverterService _sut;

    [SetUp]
    public void Setup()
    {
        _mockFactory = new Mock<ICurrencyProvidersFactory>();
        _sut = new ConverterService(_mockFactory.Object);
    }

    [Test]
    public async Task ConvertToCurrency_ShouldReturnConvertedAmount_WhenRateExists()
    {
        // Arrange
        decimal amount = 10m;
        string fromCurrency = "USD";
        string toCurrency = "EUR";

        var request = new Frankfurter.GetLatestForCurrenciesRequest(fromCurrency, new[] { toCurrency });
        var mockProvider = new Mock<ICurrencyProvider>();

        var rates = new ExchangeRates
        {
            Provider = "Frankfurter",
            Rates = new Dictionary<string, decimal> { { toCurrency, 1.23m } }
        };

        _mockFactory.Setup(f => f.GetCurrencyProvider(It.IsAny<Frankfurter.GetLatestForCurrenciesRequest>()))
            .Returns(mockProvider.Object);

        mockProvider.Setup(p => p.GetLatestForCurrenciesAsync(It.IsAny<Frankfurter.GetLatestForCurrenciesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rates);

        // Act
        var result = await _sut.ConvertToCurrency(amount, fromCurrency, toCurrency);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Provider, Is.EqualTo("Frankfurter"));
            Assert.That(result.FromCurrency, Is.EqualTo(fromCurrency));
            Assert.That(result.ToCurrency, Is.EqualTo(toCurrency));
            Assert.That(result.Amount, Is.EqualTo(Math.Round(amount * 1.23m, 2)));
        });
    }

    [Test]
    public void ConvertToCurrency_ShouldThrow_WhenCurrencyNotFound()
    {
        // Arrange
        decimal amount = 10m;
        string fromCurrency = "USD";
        string toCurrency = "UNKNOWN";

        var mockProvider = new Mock<ICurrencyProvider>();

        var rates = new ExchangeRates
        {
            Provider = "Frankfurter",
            Rates = new Dictionary<string, decimal>()
        };

        _mockFactory.Setup(f => f.GetCurrencyProvider(It.IsAny<Frankfurter.GetLatestForCurrenciesRequest>()))
            .Returns(mockProvider.Object);

        mockProvider.Setup(p => p.GetLatestForCurrenciesAsync(It.IsAny<Frankfurter.GetLatestForCurrenciesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rates);

        // Act & Assert
        Assert.ThrowsAsync<CurrencyNotFoundException>(async () =>
            await _sut.ConvertToCurrency(amount, fromCurrency, toCurrency));
    }
}
