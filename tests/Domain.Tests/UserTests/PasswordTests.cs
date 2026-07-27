using Domain.Users;

namespace Domain.Tests.UserTests;

[TestFixture]
public class PasswordTests
{
    [TestCase("P1a")]
    [TestCase("1Wg")]
    [TestCase("bA4")]
    public void Too_Short_Should_Return_Failure(string password)
    {
        var result = Password.Validate(password);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<UserDomainErrors.PasswordTooShort>());
    }

    [TestCase("MY1PASSWORD")]
    [TestCase("SOME$PASSWORD_123")]
    [TestCase("$0M3_P@$$W0RD")]
    public void Lacks_Of_Lowercase_Should_Return_Failure(string password)
    {
        var result = Password.Validate(password);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<UserDomainErrors.PasswordHasNoLowercase>());
    }

    [TestCase("my1password")]
    [TestCase("some$password_123")]
    [TestCase("$0m3_p@$$w0rd")]
    public void Lacks_Of_Uppercase_Should_Return_Failure(string password)
    {
        var result = Password.Validate(password);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<UserDomainErrors.PasswordHasNoUppercase>());
    }

    [TestCase("My_Password")]
    [TestCase("Some$Password")]
    public void Lacks_Of_Digit_Should_Return_Failure(string password)
    {
        var result = Password.Validate(password);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<UserDomainErrors.PasswordHasNoDigit>());
    }

    [TestCase("My_Password_123")]
    [TestCase("S0m3_P@ssw0rD")]
    public void Valid_Password_Should_Return_Success(string password)
    {
        var result = Password.Validate(password);

        Assert.That(result, Is.Null);
    }
}