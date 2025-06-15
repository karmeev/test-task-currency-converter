using Currency.Data.Contracts;
using Currency.Data.Contracts.Entries;
using Currency.Domain.Rates;
using Currency.Services.Application.Consumers;
using Moq;

namespace Currency.Services.Tests.Application.Consumers;

[TestFixture]
[Category("Unit")]
public class ExchangeRatesConsumerTests
{
    private Mock<IExchangeRatesRepository> _mockRatesRepo = null!;
    private Mock<IExchangeRatesHistoryRepository> _mockHistoryRepo = null!;
    private ExchangeRatesConsumer _sut = null!;

    [SetUp]
    public void Setup()
    {
        _mockRatesRepo = new Mock<IExchangeRatesRepository>();
        _mockHistoryRepo = new Mock<IExchangeRatesHistoryRepository>();
        _sut = new ExchangeRatesConsumer(_mockRatesRepo.Object, _mockHistoryRepo.Object);
    }

    [Test]
    public async Task Consume_ExchangeRatesHistory_ShouldAddRateHistory()
    {
        // Arrange
        var message = new ExchangeRatesHistory
        {
            Provider = "Prov",
            CurrentCurrency = "USD",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 2),
            Rates = new Dictionary<DateTime, Dictionary<string, decimal>>
            {
                { new DateTime(2025, 1, 1), new Dictionary<string, decimal> { { "EUR", 1.1m }, { "GBP", 0.9m } } },
                { new DateTime(2025, 1, 2), new Dictionary<string, decimal> { { "EUR", 1.2m } } }
            }
        };
        var expectedId = $"Prov:USD:{message.StartDate:yyyyMMddHHmmss}:{message.EndDate:yyyyMMddHHmmss}";

        // Act
        await _sut.Consume(message, CancellationToken.None);

        // Assert
        _mockHistoryRepo.Verify(r => r.AddRateHistory(
            expectedId,
            It.IsAny<List<ExchangeRateEntry>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Consume_ExchangeRates_ShouldAddExchangeRates()
    {
        // Arrange
        var message = new ExchangeRates
        {
            Provider = "Prov",
            CurrentCurrency = "USD"
        };
        var expectedId = "Prov:USD";

        // Act
        await _sut.Consume(message, CancellationToken.None);

        // Assert
        _mockRatesRepo.Verify(r => r.AddExchangeRates(expectedId, message, It.IsAny<CancellationToken>()), Times.Once);
    }
}
