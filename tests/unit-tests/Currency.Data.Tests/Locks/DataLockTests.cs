using Currency.Data.Contracts.Exceptions;
using Currency.Data.Locks;
using Currency.Infrastructure.Contracts.Databases.Redis;
using Moq;

namespace Currency.Data.Tests.Locks;

[TestFixture]
[Category("Unit")]
public class DataLockTests
{
    private Mock<IRedisLockContext> _contextMock;

    [SetUp]
    public void SetUp()
    {
        _contextMock = new Mock<IRedisLockContext>();
        _contextMock.SetupGet(c => c.RetryCount).Returns(3);
        _contextMock.SetupGet(c => c.RetryDelayMilliseconds).Returns(1); // keep test fast
    }

    [Test]
    public async Task AcquireLockAsync_WhenLockAcquiredOnFirstTry_ShouldSucceed()
    {
        _contextMock
            .Setup(c => c.AcquireLockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var sut = new DataLock(_contextMock.Object);
        await sut.AcquireLockAsync("lock:test");

        await sut.DisposeAsync();

        _contextMock.Verify(c => c.AcquireLockAsync("lock:test", It.IsAny<string>()), Times.Once);
        _contextMock.Verify(c => c.ReleaseLockAsync("lock:test", It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task AcquireLockAsync_WhenLockAcquiredAfterRetries_ShouldRetryThenSucceed()
    {
        _contextMock
            .SetupSequence(c => c.AcquireLockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false)
            .ReturnsAsync(false)
            .ReturnsAsync(true);

        var sut = new DataLock(_contextMock.Object);
        await sut.AcquireLockAsync("lock:test");

        await sut.DisposeAsync();

        _contextMock.Verify(c => c.AcquireLockAsync("lock:test", It.IsAny<string>()), Times.Exactly(3));
        _contextMock.Verify(c => c.ReleaseLockAsync("lock:test", It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void AcquireLockAsync_WhenLockNeverAcquired_ShouldThrowConcurrencyException()
    {
        _contextMock
            .Setup(c => c.AcquireLockAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var sut = new DataLock(_contextMock.Object);

        var ex = Assert.ThrowsAsync<ConcurrencyException>(async () =>
            await sut.AcquireLockAsync("lock:test"));

        Assert.That(ex.Message, Is.EqualTo("Could not acquire lock!"));
    }

    [Test]
    public async Task DisposeAsync_WhenNoLockAcquired_ShouldNotCallRelease()
    {
        var sut = new DataLock(_contextMock.Object);
        await sut.DisposeAsync();

        _contextMock.Verify(c => c.ReleaseLockAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}