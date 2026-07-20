using Domain.Users;

namespace Domain.Tests.UserTests;

[TestFixture]
public class UsernameTests
{

    [TestCase("SomeReallyLongUsername")]
    [TestCase("arrrrrrrrrrrrrrrrrrrrrrrrr")]
    public void TooLong_Should_Return_Failure(string username)
    {
        var result = Username.Create(username);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.EqualTo(UserErrors.UsernameIsTooLong));
    }

    [TestCase("1SomeUsername")]
    [TestCase("2short")]
    public void Starts_With_Digit_Should_Return_Failure(string username)
    {
        var result = Username.Create(username);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.EqualTo(UserErrors.UsernameStartsWithDigit));
    }

    [TestCase("_SomeUsername")]
    [TestCase("_short")]
    public void Starts_With_Underscore_Should_Return_Failure(string username)
    {
        var result = Username.Create(username);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.EqualTo(UserErrors.UsernameStartsWithUnderscore));
    }

    [TestCase("SomeUsername_")]
    [TestCase("short_")]
    public void Ends_With_Underscore_Should_Return_Failure(string username)
    {
        var result = Username.Create(username);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.EqualTo(UserErrors.UsernameEndsWithUnderscore));
    }

    [TestCase("$ome$ymbols#")]
    [TestCase("User.name")]
    [TestCase("My+Fav@Username")]
    [TestCase("User#name")]
    public void Contains_Prohibited_Symbols_Should_Return_Failure(string username)
    {
        var result = Username.Create(username);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.EqualTo(UserErrors.UsernameProhibitedSymbols));
    }

    [TestCase("Username")]
    [TestCase("Some_Username")]
    [TestCase("User123")]
    [TestCase("user_name$123")]
    public void Valid_Usernames_Should_Return_Success(string username)
    {
        var result = Username.Create(username);

        Assert.That(result.IsFailure, Is.False);
        Assert.That((string)result.Value!, Is.EqualTo(username));
    }
}