using System.Security.Cryptography;
using Domain.Users;
using Moq;

namespace Domain.Tests.UserTests;

[TestFixture]
public class PasswordTests
{
    private const int HashLength = 32;
    private Mock<IPasswordHasher> _hasherMock = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _hasherMock = new Mock<IPasswordHasher>();
        _hasherMock.Setup(x => x.Hash(It.IsAny<string>())).Returns(RandomNumberGenerator.GetHexString(HashLength));
    }

    [TestCase("P1a")]
    [TestCase("1Wg")]
    [TestCase("bA4")]
    public void Too_Short_Should_Return_Failure(string password)
    {
        var result = Password.Create(password, _hasherMock.Object);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.TypeOf<UserDomainErrors.PasswordTooShort>());
    }

    [TestCase("MY1PASSWORD")]
    [TestCase("SOME$PASSWORD_123")]
    [TestCase("$0M3_P@$$W0RD")]
    public void Lacks_Of_Lowercase_Should_Return_Failure(string password)
    {
        var result = Password.Create(password, _hasherMock.Object);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.TypeOf<UserDomainErrors.PasswordHasNoLowercase>());
    }

    [TestCase("my1password")]
    [TestCase("some$password_123")]
    [TestCase("$0m3_p@$$w0rd")]
    public void Lacks_Of_Uppercase_Should_Return_Failure(string password)
    {
        var result = Password.Create(password, _hasherMock.Object);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.TypeOf<UserDomainErrors.PasswordHasNoUppercase>());
    }

    [TestCase("My_Password")]
    [TestCase("Some$Password")]
    public void Lacks_Of_Digit_Should_Return_Failure(string password)
    {
        var result = Password.Create(password, _hasherMock.Object);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.TypeOf<UserDomainErrors.PasswordHasNoDigit>());
    }

    [TestCase("My_Password_123")]
    [TestCase("S0m3_P@ssw0rD")]
    public void Valid_Password_Should_Return_Success(string password)
    {
        var result = Password.Create(password, _hasherMock.Object);

        Assert.That(result.IsFailure, Is.False);
        Assert.That((string)result.Value!, Has.Length.EqualTo(HashLength));
    }
}