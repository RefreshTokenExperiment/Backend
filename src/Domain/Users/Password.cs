using Shared;

namespace Domain.Users;

public sealed record Password
{
    public string Value { get; }

    public const int MinLength = 4;

    private Password(string value)
    {
        Value = value;
    }

    public static Result<Password> Create(string password, IPasswordHasher hasher)
    {
        var validationError = Validate(password);
        return validationError is null ? new Password(hasher.Hash(password)) : validationError;
    }

    private static Error? Validate(string password)
    {
        if (password.Length < MinLength) return UserErrors.PasswordTooShort;
        if (!password.Any(char.IsLower)) return UserErrors.PasswordHasNoLowercase;
        if (!password.Any(char.IsUpper)) return UserErrors.PasswordHasNoUppercase;
        if (!password.Any(char.IsDigit)) return UserErrors.PasswordHasNoDigit;

        return null;
    }

    public static implicit operator string(Password password)
        => password.Value;
}