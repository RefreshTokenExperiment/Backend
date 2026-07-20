using Domain.Users;

namespace Domain.Tests.UserTests;

public class EmailTests
{
    [TestCase("hello@world")]
    [TestCase("some-world@hi")]
    [TestCase("another.invalid@mail.")]
    [TestCase("just?invalid-mail")]
    public void InvalidEmails_Should_Return_Failure(string invalidEmail)
    {
        var result = Email.Create(invalidEmail);

        Assert.That(result.IsFailure, Is.True);
    }

    [TestCase("some-valid@mail.com")]
    [TestCase("another.valid@mail.com")]
    [TestCase("my_favorite-valid.email@mail.organization.org")]
    public void ValidEmails_Should_Return_Success(string invalidEmail)
    {
        var result = Email.Create(invalidEmail);

        Assert.That(result.IsFailure, Is.False);
    }

    [Test]
    public void Implicit_To_String_Should_Return_String()
    {
        const string email = "my-email@mail.com";
        var emailResult = Email.Create(email);
        Assert.That(emailResult.IsFailure, Is.False);

        string value = emailResult.Value!;
        
        Assert.That(value, Is.EqualTo(email));
    }
}
