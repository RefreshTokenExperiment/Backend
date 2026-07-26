using Shared;
using System.Text.RegularExpressions;

namespace Domain.Users;

public sealed partial record Email
{
    public string Value { get; init; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string email)
    {
        var validationError = Validate(email);
        return validationError is null ? new Email(email) : validationError;
    }

    private static Error? Validate(string email)
        => EmailRegex().IsMatch(email) ? null : new UserDomainErrors.EmailIsInvalid();

    public static implicit operator string(Email email)
        => email.Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}