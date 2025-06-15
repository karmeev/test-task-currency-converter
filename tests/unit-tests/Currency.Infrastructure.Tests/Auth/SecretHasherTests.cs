using Currency.Infrastructure.Auth;
using Currency.Infrastructure.Tests.Utility;

namespace Currency.Infrastructure.Tests.Auth;

[Category("Unit")]
public class SecretHasherTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Hash_HappyPath_ShouldReturnHashedString()
    {
        Test.StartTest();
        
        //Arrange
        var sut = new SecretHasher();

        //Act
        var encoded = sut.Hash("my_test_password");
        var encoded2 = sut.Hash("my_test_password_2");

        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.Not.Null.Or.Empty);
            Assert.That(encoded2, Is.Not.Null.Or.Empty);
        });
        
        Test.CompleteTest();
    }
}