using Currency.Infrastructure.Auth;
using Currency.Infrastructure.Tests.Utility;

namespace Currency.Infrastructure.Tests.Auth;

[Category("Unit")]
public class SecretHasherTests
{
    private SecretHasher sut;

    [SetUp]
    public void Setup()
    {
        sut = new SecretHasher();
    }

    [Test]
    public void Hash_HappyPath_ShouldReturnHashedString()
    {
        var encoded = sut.Hash("my_test_password");
        var encoded2 = sut.Hash("my_test_password_2");

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.Not.Null.Or.Empty);
            Assert.That(encoded2, Is.Not.Null.Or.Empty);
        });
    }

    [Test]
    public void Verify_WithCorrectSecret_ShouldReturnTrue()
    {
        var secret = "super_secret_password";
        var hash = sut.Hash(secret);

        var result = sut.Verify(secret, hash);

        Assert.That(result, Is.True);
    }

    [Test]
    public void Verify_WithIncorrectSecret_ShouldReturnFalse()
    {
        var secret = "super_secret_password";
        var wrongSecret = "not_the_password";
        var hash = sut.Hash(secret);

        var result = sut.Verify(wrongSecret, hash);

        Assert.That(result, Is.False);
    }

    [Test]
    public void Hash_SameSecret_Twice_ShouldProduceDifferentHashes()
    {
        var secret = "same_secret";

        var hash1 = sut.Hash(secret);
        var hash2 = sut.Hash(secret);

        Assert.That(hash1, Is.Not.EqualTo(hash2),
            "Hashes should differ due to random salt.");
    }
}
