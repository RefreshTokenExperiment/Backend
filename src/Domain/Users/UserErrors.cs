using Shared;

namespace Domain.Users;

public record UserDomainErrors(string Code, string Message) : Error(Code, Message)
{
    // Email Errors
    public sealed record EmailIsInvalid() : UserDomainErrors("Email.IsInvalid", "Given Email Address has invalid format.");

    // Username Errors
    public sealed record UsernameIsTooLong(): UserDomainErrors("Username.IsTooLong", $"Given username length is greater than {Username.MaxLength} symbols.");
    public sealed record UsernameStartsWithDigit(): UserDomainErrors("Username.StartsWithDigit", "Given username starts with a digit that is prohibited.");
    public sealed record UsernameStartsWithUnderscore(): UserDomainErrors("Username.StartsWithUnderscore", "Given username starts with an underscore that is prohibited.");
    public sealed record UsernameEndsWithUnderscore(): UserDomainErrors("Username.EndsWithUnderscore", "Given username ends with an underscore that is prohibited.");
    public sealed record UsernameProhibitedSymbols(): UserDomainErrors("Username.ProhibitedSymbols", "Given username contains prohibited symbols.");


    // Password Errors
    public sealed record PasswordTooShort() : UserDomainErrors("Password.TooShort", $"Given password length is less than {Password.MinLength} symbols.");
    public sealed record PasswordHasNoLowercase() : UserDomainErrors("Password.HasNoLowercase", "Given password does not contain any lowercase letter.");
    public sealed record PasswordHasNoUppercase() : UserDomainErrors("Password.HasNoUppercase", "Given password does not contain any uppercase letter.");
    public sealed record PasswordHasNoDigit() : UserDomainErrors("Password.HasNoDigit", "Given password does not contain any digit.");
}