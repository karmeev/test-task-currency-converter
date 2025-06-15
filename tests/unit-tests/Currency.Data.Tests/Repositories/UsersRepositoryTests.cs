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
    public async Task GetUserByUsernameAsync_UserExists_ReturnsUser()
    {
        var model = new LoginModel("john", "pwd");
        var user = new User();
        _mockContext.Setup(x => x.TryGetByIndexAsync<User>("user:user-by-username:john")).ReturnsAsync(user);

        var result = await _sut.GetUserByUsernameAsync(model, CancellationToken.None);

        Assert.That(result, Is.EqualTo(user));
    }

    [Test]
    public void GetUserByUsernameAsync_NotFound_Throws()
    {
        var model = new LoginModel("ghost", "pwd");
        _mockContext.Setup(x => x.TryGetByIndexAsync<User>("user:user-by-username:ghost")).ReturnsAsync((User)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.GetUserByUsernameAsync(model, CancellationToken.None));
    }

    [Test]
    public async Task GetUserByIdAsync_ReturnsUser()
    {
        var user = new User();
        _mockContext.Setup(x => x.TryGetByIndexAsync<User>("user:user-by-id:42")).ReturnsAsync(user);

        var result = await _sut.GetUserByIdAsync("42", CancellationToken.None);

        Assert.That(result, Is.EqualTo(user));
    }
}

