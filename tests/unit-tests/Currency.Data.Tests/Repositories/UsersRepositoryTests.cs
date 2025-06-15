using Currency.Data.Contracts;
using Currency.Data.Contracts.Exceptions;
using Currency.Data.Repositories;
using Currency.Domain.Login;
using Currency.Domain.Users;
using Currency.Infrastructure.Contracts.Databases.Redis;
using Moq;

namespace Currency.Data.Tests.Repositories;

[Category("Unit")]
public class UsersRepositoryTests
{
    private Mock<IRedisContext> _mockContext;
    private IUsersRepository _sut;

    [SetUp]
    public void Setup()
    {
        _mockContext = new Mock<IRedisContext>();
        _sut = new UsersRepository(_mockContext.Object);
    }

    [Test]
    public async Task GetUserByUsernameAsync_Found_ReturnsUser()
    {
        // Arrange
        var login = new LoginModel("john", "murdak");
        var expectedUser = new User();
        _mockContext.Setup(x => x.TryGetByIndexAsync<User>("user:user-by-username:john"))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _sut.GetUserByUsernameAsync(login, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(expectedUser));
    }

    [Test]
    public void GetUserByUsernameAsync_NotFound_Throws()
    {
        // Arrange
        var login = new LoginModel("notfound", "murdak");
        _mockContext.Setup(x => x.TryGetByIndexAsync<User>("user:user-by-username:notfound"))
            .ReturnsAsync((User)null);

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(async () =>
            await _sut.GetUserByUsernameAsync(login, CancellationToken.None));
    }
}
