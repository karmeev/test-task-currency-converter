using System.Security.Claims;
using Bogus;
using Currency.Domain.Login;
using Currency.Domain.Users;
using Currency.Facades.Tests.Fakes;
using Currency.Facades.Tests.Utility;
using Currency.Facades.Validators;
using Currency.Services.Contracts.Application;
using Microsoft.Extensions.Logging;
using Moq;
using ValidationResult = Currency.Facades.Validators.Results.ValidationResult;

namespace Currency.Facades.Tests;

[TestFixture]
[Category("Unit")]
public class AuthFacadeTests
{
    private ILogger<AuthFacade> _logger;
    
    [SetUp]
    public void Setup()
    {
        _userService = new Mock<IUserService>();
        _tokenService = new Mock<ITokenService>();
        _logger = Test.GetLogger<AuthFacade>();
    }
    
    private Mock<IUserService> _userService;
    private Mock<ITokenService> _tokenService;

    [Test]
    public async Task LoginAsync_HappyPath_ShouldReturnTokens()
    {
        Test.StartTest();
        
        //Arrange
        var request = FakeRequests.GenerateLoginRequest();

        var user = new User
        {
            Id = "1",
            Username = request.Username,
            Password = request.Password,
            Role = UserRole.User
        };

        _userService.Setup(x => x.GetUserAsync(It.IsAny<LoginModel>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);

        _tokenService.Setup(x => x.GenerateTokens(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns((FakeResults.GenerateFakeTokens(), new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.Username)
            }));
        _tokenService.Setup(x => x.AddRefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));

        var sut = new AuthFacade(_userService.Object, _tokenService.Object, _logger);

        //Act
        var result = await sut.LoginAsync(request, CancellationToken.None);

        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ErrorMessage, Is.Empty);
            Assert.That(result.Claims, Is.Not.Null);
            Assert.That(result.Claims.Count, Is.EqualTo(2));
            Assert.That(result.Claims.First(x => x.Type == ClaimTypes.Name).Value, Is.EqualTo(request.Username));
            Assert.That(result.AccessToken, Is.Not.Null.Or.Empty);
            Assert.That(result.RefreshToken, Is.Not.Null.Or.Empty);
        });
        
        Test.CompleteTest();
    }

    [Test]
    public async Task RefreshTokenAsync_HappyPath_ShouldReturnTokens()
    {
        Test.StartTest();
        
        //Arrange
        var request = new Faker().Random.Hash();

        var user = new User
        {
            Id = new Faker().Random.Number(1000).ToString(),
            Username = new Faker().Person.UserName,
            Password = new Faker().Random.Hash(),
            Role = UserRole.User
        };

        _userService.Setup(x => x.TryGetUserByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);

        _tokenService.Setup(x => x.GetRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshToken { Verified = true, UserId = user.Id });
        _tokenService.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns((
                new AccessToken { ExpiresAt = DateTime.MaxValue, Token = new Faker().Random.Hash() },
                new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.Id),
                    new(ClaimTypes.Name, user.Username)
                }));

        var sut = new AuthFacade(_userService.Object, _tokenService.Object, _logger);

        //Act
        var result = await sut.RefreshTokenAsync(request, CancellationToken.None);

        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ErrorMessage, Is.Empty);
            Assert.That(result.Claims, Is.Not.Null);
            Assert.That(result.Claims.Count, Is.EqualTo(2));
            Assert.That(result.Claims.First(x => x.Type == ClaimTypes.Name).Value, Is.EqualTo(user.Username));
            Assert.That(result.AccessToken, Is.Not.Null.Or.Empty);
            Assert.That(result.RefreshToken, Is.Not.Null.Or.Empty);
        });
        
        Test.CompleteTest();
    }
}