using Currency.Data.Contracts;
using Currency.Data.Models;
using Currency.Data.Repositories;
using Currency.Domain.Login;
using Currency.Infrastructure.Contracts.Databases.Redis;
using Moq;

namespace Currency.Data.Tests.Repositories;

[Category("Unit")]
public class AuthRepositoryTests
{
    private Mock<IRedisContext> _mockContext;
    private IAuthRepository _sut;

    [SetUp]
    public void Setup()
    {
        _mockContext = new Mock<IRedisContext>();
        _sut = new AuthRepository(_mockContext.Object);
    }

    [Test]
    public async Task AddRefreshToken_CancellationRequested_ShouldNotCallSetAsync()
    {
        var token = new RefreshToken { Token = "token123", ExpiresAt = TimeSpan.FromMinutes(5) };
        var ct = new CancellationToken(true); // canceled

        await _sut.AddRefreshToken(token, ct);

        _mockContext.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Test]
    public async Task AddRefreshToken_ShouldCallSetAsync()
    {
        var token = new RefreshToken { Token = "token123", ExpiresAt = TimeSpan.FromMinutes(5) };

        await _sut.AddRefreshToken(token, CancellationToken.None);

        _mockContext.Verify(x =>
            x.SetAsync($"auth:{token.Token}", token, token.ExpiresAt), Times.Once);
    }

    [Test]
    public async Task GetRefreshTokenAsync_TokenExists_ReturnsMapped()
    {
        var model = new RefreshTokenModel
        {
            Verified = true,
            ExpirationDate = DateTime.UtcNow,
            ExpiresAt = TimeSpan.FromMinutes(30),
            UserId = "user-id"
        };

        _mockContext.Setup(x => x.TryGetAsync<RefreshTokenModel>("auth:abc")).ReturnsAsync(model);

        var result = await _sut.GetRefreshTokenAsync("abc", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Token, Is.EqualTo("abc"));
            Assert.That(result.Verified, Is.True);
            Assert.That(result.UserId, Is.EqualTo("user-id"));
        });
    }

    [Test]
    public async Task GetRefreshTokenAsync_NotExists_ReturnsEmpty()
    {
        _mockContext.Setup(x => x.TryGetAsync<RefreshTokenModel>("auth:abc")).ReturnsAsync((RefreshTokenModel)null);

        var result = await _sut.GetRefreshTokenAsync("abc", CancellationToken.None);

        Assert.That(result.Verified, Is.False);
        Assert.That(result.Token, Is.Null);
    }
}

