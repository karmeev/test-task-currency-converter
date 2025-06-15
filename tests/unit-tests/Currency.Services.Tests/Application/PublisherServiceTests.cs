using System.Threading.Channels;
using Autofac;
using Currency.Domain.Operations;
using Currency.Services.Application;
using Moq;

namespace Currency.Services.Tests.Application;

[TestFixture]
public class PublisherServiceTests
{
    private Mock<ILifetimeScope> _mockScope = null!;
    private PublisherService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _mockScope = new Mock<ILifetimeScope>();
        _sut = new PublisherService(_mockScope.Object);
    }

    [Test]
    public async Task Publish_ShouldWriteToChannel_WhenNotCancelled()
    {
        // Arrange
        var message = new CurrencyConversion();
        var mockWriter = new Mock<ChannelWriter<CurrencyConversion>>();
        _mockScope.Setup(s => s.Resolve<ChannelWriter<CurrencyConversion>>()).Returns(mockWriter.Object);

        // Act
        await _sut.Publish(message, CancellationToken.None);

        // Assert
        mockWriter.Verify(w => w.WriteAsync(message, CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task Publish_ShouldReturnImmediately_WhenCancelled()
    {
        // Arrange
        var token = new CancellationToken(true);
        var message = new CurrencyConversion();

        // Act
        await _sut.Publish(message, token);

        // Assert
        _mockScope.Verify(s => s.Resolve<ChannelWriter<CurrencyConversion>>(), Times.Never);
    }
}
