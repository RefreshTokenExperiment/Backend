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

    public static Password FromHash(string passwordHash) => new(passwordHash);

    private static Error? Validate(string password)
    {
        if (password.Length < MinLength) return new UserDomainErrors.PasswordTooShort();
        if (!password.Any(char.IsLower)) return new UserDomainErrors.PasswordHasNoLowercase();
        if (!password.Any(char.IsUpper)) return new UserDomainErrors.PasswordHasNoUppercase();
        if (!password.Any(char.IsDigit)) return new UserDomainErrors.PasswordHasNoDigit();

        return null;
    }

    public static implicit operator string(Password password)
        => password.Value;
}