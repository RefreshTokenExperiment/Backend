using Domain.Users;
using Infrastructure.Services;

namespace Infrastructure.Tests.Services;

[TestFixture]
public sealed class BCryptPasswordHasherTests
{
    private BCryptPasswordHasher _hasher = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _hasher = new();
    }

    [Test]
    public void Verify_InvalidPassword_ReturnsFailure()
    {
        const string password = "SomePassword1234";
        var hash = _hasher.Hash(password);
        var hashedPassword = Password.FromHash(hash);

        var result = _hasher.Verify("InvalidPassword", hashedPassword);

        Assert.That(result, Is.False);
    }

    [Test]
    public void Verify_ValidPassword_ReturnsSuccess()
    {
        const string password = "SomePassword1234";
        var hash = _hasher.Hash(password);
        var hashedPassword = Password.FromHash(hash);

        var result = _hasher.Verify(password, hashedPassword);

        Assert.That(result, Is.True);
    }
}