using Currency.Facades.Validators;

namespace Currency.Facades.Tests.Validators;

[Category("Unit")]
public class AuthValidatorTests
{
    [TestCase(null, "password", false, TestName = "UsernameNull_ShouldFail")]
    [TestCase("username", null, false, TestName = "PasswordNull_ShouldFail")]
    [TestCase("", "password", false, TestName = "UsernameEmpty_ShouldFail")]
    [TestCase("username", "", false, TestName = "PasswordEmpty_ShouldFail")]
    [TestCase("   ", "   ", false, TestName = "WhitespaceOnly_ShouldFail")]
    [TestCase("validUser", "validPass", true, TestName = "ValidCredentials_ShouldSucceed")]
    public void Validate_ShouldReturnExpectedResult(string username, string password, bool expectedSuccess)
    {
        // Act
        var result = AuthValidator.Validate(username, password);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.EqualTo(expectedSuccess));
            if (!expectedSuccess)
                Assert.That(result.Message, Is.EqualTo("Username and password are required."));
        });
    }
}
