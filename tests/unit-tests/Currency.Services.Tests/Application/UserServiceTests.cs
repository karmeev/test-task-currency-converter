using Bogus;
using Currency.Data.Contracts;
using Currency.Domain.Login;
using Currency.Infrastructure.Contracts.Auth;
using Currency.Services.Application;
using Currency.Services.Tests.Fakes;
using Currency.Services.Tests.Utility;
using Microsoft.Extensions.Logging;
using Moq;

namespace Currency.Services.Tests.Application;

[Category("Unit tests")]
public class UserServiceTests
{
    private Mock<ISecretHasher> _secretHasherMock;
    private Mock<IUsersRepository> _userRepositoryMock;
    private ILogger<UserService> _logger;

    [SetUp]
    public void Setup()
    {
        _secretHasherMock = new Mock<ISecretHasher>();
        _userRepositoryMock = new Mock<IUsersRepository>();
        _logger = Test.GetLogger<UserService>();
    }

    [Test]
    public async Task TryGetUserAsync_HappyPath_ShouldReturnUser()
    {
        Test.StartTest();
        
        //Arrange
        var user = FakeModels.GenerateFakeUser();
        var model = new LoginModel(user.Username, string.Empty);

        _secretHasherMock.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        _userRepositoryMock.Setup(x => x.GetUserByUsernameAsync(It.IsAny<LoginModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = new UserService(_logger, _secretHasherMock.Object, _userRepositoryMock.Object);

        //Act
        var result = await sut.GetUserAsync(model, CancellationToken.None);

        //Assert
        if (result is not null)
        {
            Test.CompleteTest();
            Assert.Pass();
        }
        else
        {
            Test.CompleteWithFailTest();
            Assert.Fail();
        }
    }

    [Test]
    public async Task TryGetUserByIdAsync_HappyPath_ShouldReturnUser()
    {
        Test.StartTest();
        
        //Arrange
        var id = new Faker().Random.Hash();
        var user = FakeModels.GenerateFakeUser();
        user.Id = id;

        _userRepositoryMock.Setup(x => x.GetUserByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = new UserService(_logger, _secretHasherMock.Object, _userRepositoryMock.Object);

        //Act
        var result = await sut.TryGetUserByIdAsync(id, CancellationToken.None);

        //Assert
        if (result is not null)
        {
            Test.CompleteTest();
            Assert.Pass();
        }
        else
        {
            Test.CompleteWithFailTest();
            Assert.Fail();
        }
    }
}