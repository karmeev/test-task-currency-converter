using Currency.Data.Contracts;
using Currency.Domain.Operations;
using Currency.Services.Application.Consumers;
using Moq;

namespace Currency.Services.Tests.Application.Consumers;

[TestFixture]
[Category("Unit")]
public class CurrencyConversionConsumerTests
{
    private Mock<IExchangeRatesRepository> _mockRepo = null!;
    private CurrencyConversionConsumer _sut = null!;

    [SetUp]
    public void Setup()
    {
        _mockRepo = new Mock<IExchangeRatesRepository>();
        _sut = new CurrencyConversionConsumer(_mockRepo.Object);
    }

    [Test]
    public async Task Consume_ShouldCallRepositoryWithCorrectId()
    {
        // Arrange
        var message = new CurrencyConversion
        {
            Provider = "Prov",
            FromCurrency = "USD",
            ToCurrency = "EUR",
            Amount = 123.45m
        };
        var expectedId = "Prov:USD:EUR";

        // Act
        await _sut.Consume(message, CancellationToken.None);

        // Assert
        _mockRepo.Verify(r => r.AddConversionResultAsync(expectedId, message, It.IsAny<CancellationToken>()), Times.Once);
    }
}
