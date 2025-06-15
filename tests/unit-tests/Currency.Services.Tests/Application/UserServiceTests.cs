using Bogus;
using Currency.Data.Contracts;
using Currency.Data.Contracts.Exceptions;
using Currency.Domain.Login;
using Currency.Infrastructure.Contracts.Auth;
using Currency.Services.Application;
using Currency.Services.Tests.Fakes;
using Currency.Services.Tests.Utility;
using Microsoft.Extensions.Logging;
using Moq;

namespace Currency.Services.Tests.Application;

[Category("Unit")]
public class UserServiceTests
{
    private Mock<ISecretHasher> _secretHasherMock = null!;
    private Mock<IUsersRepository> _userRepositoryMock = null!;
    private ILogger<UserService> _logger = null!;

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

        var user = FakeModels.GenerateFakeUser();
        var model = new LoginModel(user.Username, string.Empty);

        _secretHasherMock.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _userRepositoryMock.Setup(x => x.GetUserByUsernameAsync(It.IsAny<LoginModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = new UserService(_logger, _secretHasherMock.Object, _userRepositoryMock.Object);

        var result = await sut.GetUserAsync(model, CancellationToken.None);

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
    public async Task TryGetUserAsync_WhenPasswordVerificationFails_ShouldReturnNull()
    {
        Test.StartTest();

        var user = FakeModels.GenerateFakeUser();
        var model = new LoginModel(user.Username, "wrongpassword");

        _secretHasherMock.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        _userRepositoryMock.Setup(x => x.GetUserByUsernameAsync(It.IsAny<LoginModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = new UserService(_logger, _secretHasherMock.Object, _userRepositoryMock.Object);

        var result = await sut.GetUserAsync(model, CancellationToken.None);

        Assert.That(result, Is.Null);

        Test.CompleteTest();
    }

    [Test]
    public void TryGetUserAsync_WhenUserNotFound_ShouldLogAndThrow()
    {
        Test.StartTest();

        var model = new LoginModel("nonexistent", "password");
        var exception = new NotFoundException("User not found");

        _userRepositoryMock.Setup(x => x.GetUserByUsernameAsync(It.IsAny<LoginModel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var sut = new UserService(_logger, _secretHasherMock.Object, _userRepositoryMock.Object);

        var ex =
            Assert.ThrowsAsync<NotFoundException>(async () => await sut.GetUserAsync(model, CancellationToken.None));
        Assert.That(ex, Is.EqualTo(exception));

        Test.CompleteTest();
    }
}
