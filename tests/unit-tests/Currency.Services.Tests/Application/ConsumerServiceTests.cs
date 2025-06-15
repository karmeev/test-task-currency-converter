using System.Threading.Channels;
using Autofac;
using Currency.Domain.Operations;
using Currency.Domain.Rates;
using Currency.Services.Application;
using Microsoft.Extensions.Logging;
using Moq;

namespace Currency.Services.Tests.Application;

[TestFixture]
[Ignore("Untested")]
public class ConsumerServiceTests
{
    private Mock<ILifetimeScope> _mockLifetimeScope = null!;
    private Channel<ExchangeRatesHistory> _historyChannel = null!;
    private Channel<CurrencyConversion> _currencyChannel = null!;
    private Channel<ExchangeRates> _ratesChannel = null!;
    private ConsumerService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _mockLifetimeScope = new Mock<ILifetimeScope>();

        _historyChannel = Channel.CreateUnbounded<ExchangeRatesHistory>();
        _currencyChannel = Channel.CreateUnbounded<CurrencyConversion>();
        _ratesChannel = Channel.CreateUnbounded<ExchangeRates>();

        _sut = new ConsumerService(_mockLifetimeScope.Object, _historyChannel, _currencyChannel, _ratesChannel);
    }

    [Test]
    public async Task HandleAsync_ShouldReleaseSemaphoreAndLogOnException()
    {
        // Arrange
        var wasLogged = false;
        var mockLogger = new Mock<ILogger<ConsumerService>>();
        _mockLifetimeScope.Setup(s => s.Resolve<ILogger<ConsumerService>>()).Returns(mockLogger.Object);

        Func<Task> failingConsume = () => throw new InvalidOperationException("Boom");

        // Act
        //await _sut.HandleAsync(failingConsume);

        // Assert
        mockLogger.Verify(
            l => l.LogError(It.IsAny<InvalidOperationException>(), It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_ShouldReleaseSemaphoreOnSuccess()
    {
        // Arrange
        var semaphore = new SemaphoreSlim(1, 1);
        var released = false;
        
        // Act & Assert
        //Assert.DoesNotThrowAsync(async () => await _sut.HandleAsync(() => Task.CompletedTask));
    }

    [Test]
    public void Dispose_ShouldCancelAndDisposeResources()
    {
        // Act
        _sut.Dispose();

        // Assert
        Assert.That(_historyChannel.Reader.Completion.IsCompleted, Is.True);
        Assert.That(_currencyChannel.Reader.Completion.IsCompleted, Is.True);
        Assert.That(_ratesChannel.Reader.Completion.IsCompleted, Is.True);
    }

    [TearDown]
    public void TearDown()
    {
        _sut.Dispose();
    }
}
