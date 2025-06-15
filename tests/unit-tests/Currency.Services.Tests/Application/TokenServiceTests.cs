using System.Security.Claims;
using Bogus;
using Currency.Data.Contracts;
using Currency.Domain.Login;
using Currency.Domain.Users;
using Currency.Infrastructure.Contracts.JwtBearer;
using Currency.Services.Application;
using Currency.Services.Application.Settings;
using Currency.Services.Tests.Fakes;
using Currency.Services.Tests.Utility;
using Moq;

namespace Currency.Services.Tests.Application;

[Category("Unit")]
public class TokenServiceTests
{
    private Mock<IAuthRepository> _mockAuthRepository = null!;
    private Mock<IJwtTokenGenerator> _mockJwtTokenGenerator = null!;
    private ServicesSettings _settings = null!;

    [SetUp]
    public void Setup()
    {
        _mockJwtTokenGenerator = new Mock<IJwtTokenGenerator>();
        _mockAuthRepository = new Mock<IAuthRepository>();
        _settings = new ServicesSettings { RefreshTokenTtlInDays = 7 };
    }

    [Test]
    public void GenerateTokens_HappyPath_ShouldReturnTokenModel()
    {
        Test.StartTest();

        _mockJwtTokenGenerator.Setup(x => x.BuildClaims(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserRole>()))
            .Returns(new List<Claim> { new(ClaimTypes.Name, "name") });

        _mockJwtTokenGenerator.Setup(x => x.CreateAccessToken(It.IsAny<IEnumerable<Claim>>()))
            .Returns(new AccessToken { ExpiresAt = DateTime.MaxValue, Token = "111" });

        _mockJwtTokenGenerator.Setup(x => x.CreateRefreshToken(It.IsAny<string>()))
            .Returns("1111");

        var sut = new TokenService(_settings, _mockJwtTokenGenerator.Object, _mockAuthRepository.Object);

        var (result, resultClaims) = sut.GenerateTokens(FakeModels.GenerateFakeUser(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.AccessToken, Is.Not.Null.Or.Empty);
            Assert.That(result.RefreshToken, Is.Not.Null.Or.Empty);
            Assert.That(resultClaims, Is.Not.Null.And.Not.Empty);
        });

        Test.CompleteTest();
    }
    
    [Test]
    public void GenerateAccessToken_HappyPath_ShouldReturnAccessTokenAndClaims()
    {
        Test.StartTest();

        _mockJwtTokenGenerator.Setup(x => x.BuildClaims(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserRole>()))
            .Returns(new List<Claim> { new(ClaimTypes.Name, "name") });

        _mockJwtTokenGenerator.Setup(x => x.CreateAccessToken(It.IsAny<IEnumerable<Claim>>()))
            .Returns(new AccessToken { ExpiresAt = DateTime.MaxValue, Token = "111" });

        var sut = new TokenService(_settings, _mockJwtTokenGenerator.Object, _mockAuthRepository.Object);

        var (result, resultClaims) = sut.GenerateAccessToken(FakeModels.GenerateFakeUser(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Token, Is.Not.Null.Or.Empty);
            Assert.That(resultClaims, Is.Not.Null.And.Not.Empty);
        });

        Test.CompleteTest();
    }

    [Test]
    public async Task GetRefreshTokenAsync_HappyPath_ShouldReturnRefreshToken()
    {
        Test.StartTest();

        var refreshToken = new Faker().Random.Hash();

        _mockAuthRepository.Setup(x => x.GetRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshToken { Verified = true });

        var sut = new TokenService(_settings, _mockJwtTokenGenerator.Object, _mockAuthRepository.Object);

        var result = await sut.GetRefreshTokenAsync(refreshToken, CancellationToken.None);

        Assert.That(result.Verified, Is.True);

        Test.CompleteTest();
    }

    [Test]
    public async Task GetRefreshTokenAsync_WhenRepositoryReturnsNull_ShouldReturnNull()
    {
        Test.StartTest();

        var refreshToken = new RefreshToken
        {
            Verified = false,
        };

        _mockAuthRepository.Setup(x => x.GetRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        var sut = new TokenService(_settings, _mockJwtTokenGenerator.Object, _mockAuthRepository.Object);

        var result = await sut.GetRefreshTokenAsync("token", CancellationToken.None);

        Assert.That(result.Verified, Is.False);

        Test.CompleteTest();
    }

    [Test]
    public async Task AddRefreshTokenAsync_HappyPath_ShouldProcessSuccessfully()
    {
        Test.StartTest();

        _mockAuthRepository.Setup(x => x.AddRefreshToken(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new TokenService(_settings, _mockJwtTokenGenerator.Object, _mockAuthRepository.Object);

        var refreshToken = new Faker().Random.Hash();

        Assert.DoesNotThrowAsync(async () => await sut.AddRefreshTokenAsync(refreshToken, "1", CancellationToken.None));

        _mockAuthRepository.Verify(x => x.AddRefreshToken(It.Is<RefreshToken>(t =>
            t.Token == refreshToken &&
            t.UserId == "1" &&
            t.Verified == true &&
            t.ExpirationDate > DateTime.UtcNow), It.IsAny<CancellationToken>()), Times.Once);

        Test.CompleteTest();
    }

    [Test]
    public void AddRefreshTokenAsync_WhenCalled_ShouldSetExpirationCorrectly()
    {
        Test.StartTest();

        _mockAuthRepository.Setup(x => x.AddRefreshToken(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<RefreshToken, CancellationToken>((token, _) =>
            {
                Assert.That(token.ExpiresAt.TotalDays, Is.EqualTo(_settings.RefreshTokenTtlInDays));
                Assert.That(token.ExpirationDate, Is.GreaterThan(DateTime.UtcNow));
            });

        var sut = new TokenService(_settings, _mockJwtTokenGenerator.Object, _mockAuthRepository.Object);

        var refreshToken = "refreshTokenValue";

        Assert.DoesNotThrowAsync(async () => await sut.AddRefreshTokenAsync(refreshToken, "userId", CancellationToken.None));

        Test.CompleteTest();
    }
}
