using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Currency.Domain.Users;
using Currency.Infrastructure.JwtBearer;
using Currency.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Moq;

namespace Currency.Infrastructure.Tests.JwtBearer;

[TestFixture]
[Category("Unit")]
public class JwtTokenGeneratorTests
{
    private InfrastructureSettings _infrastructureSettings = null!;
    private JwtTokenGenerator _sut = null!;

    [SetUp]
    public void Setup()
    {
        var jwtSettings = new JwtSettings
        {
            SecurityKey = "super_secret_security_key_which_is_long_enough",
            Issuer = "myIssuer",
            Audience = "myAudience",
            AccessTokenTtlInMinutes = 60
        };

        var mockJwtOptions = new Mock<IOptionsMonitor<JwtSettings>>();
        mockJwtOptions.Setup(o => o.CurrentValue).Returns(jwtSettings);

        // For Redis and Frankfurter, just dummy mocks, since we don’t test them here
        var mockRedisOptions = new Mock<IOptionsMonitor<RedisSettings>>();
        mockRedisOptions.Setup(o => o.CurrentValue).Returns(new RedisSettings());

        var mockFrankfurterOptions = new Mock<IOptionsMonitor<FrankfurterSettings>>();
        mockFrankfurterOptions.Setup(o => o.CurrentValue).Returns(new FrankfurterSettings());

        _infrastructureSettings = new InfrastructureSettings(
            mockJwtOptions.Object,
            mockRedisOptions.Object,
            mockFrankfurterOptions.Object);

        _sut = new JwtTokenGenerator(_infrastructureSettings);
    }

    [Test]
    public void BuildClaims_ShouldReturnCorrectClaims()
    {
        // Arrange
        var userId = "user-123";
        var username = "myuser";
        var role = UserRole.Admin;

        // Act
        var claims = _sut.BuildClaims(userId, username, role).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(claims.Any(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId));
            Assert.That(claims.Any(c => c.Type == ClaimTypes.Name && c.Value == username));
            Assert.That(claims.Any(c => c.Type == ClaimTypes.Role && c.Value == role.ToString()));
            Assert.That(claims.Any(c => c.Type == JwtRegisteredClaimNames.Jti && !string.IsNullOrEmpty(c.Value)));
        });
    }

    [Test]
    public void CreateAccessToken_ShouldCreateValidJwtToken()
    {
        // Arrange
        var claims = _sut.BuildClaims("id", "user", UserRole.User);

        // Act
        var token = _sut.CreateAccessToken(claims);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(token.Token, Is.Not.Null.Or.Empty);

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token.Token);

            Assert.That(jwt.Issuer, Is.EqualTo(_infrastructureSettings.JwtSettings.Issuer));
            Assert.That(jwt.Audiences.Contains(_infrastructureSettings.JwtSettings.Audience));

            Assert.That(jwt.Claims.Any(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "id"));
            Assert.That(jwt.Claims.Any(c => c.Type == ClaimTypes.Name && c.Value == "user"));
            Assert.That(jwt.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == UserRole.User.ToString()));

            var expectedExpiry = DateTime.UtcNow.AddMinutes(_infrastructureSettings.JwtSettings.AccessTokenTtlInMinutes);
            Assert.That(jwt.ValidTo, Is.EqualTo(token.ExpiresAt).Within(60).Seconds);
            Assert.That(token.ExpiresAt, Is.InRange(DateTime.UtcNow, expectedExpiry.AddMinutes(1)));
        });
    }

    [Test]
    public void CreateRefreshToken_ShouldReturnBase64StringAndBeUnique()
    {
        // Act
        var token1 = _sut.CreateRefreshToken("user1");
        var token2 = _sut.CreateRefreshToken("user2");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(token1, Is.Not.Null.Or.Empty);
            Assert.That(token2, Is.Not.Null.Or.Empty);
            Assert.That(token1, Is.Not.EqualTo(token2));

            Assert.That(token1.Length, Is.EqualTo(88)); // 64 bytes in Base64
            Assert.That(token2.Length, Is.EqualTo(88));
        });
    }
}