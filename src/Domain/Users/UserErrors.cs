using Shared;

namespace Domain.Users;

public static class UserErrors
{
    // Email Errors
    public static Error EmailIsInvalid => new("Email.IsInvalid", "Given Email Address is invalid.");

    // Username Errors
    public static Error UsernameIsTooLong => new("Username.IsTooLong", $"Given username length is greater than {Username.MaxLength} symbols.");
    public static Error UsernameStartsWithDigit => new("Username.StartsWithDigit", "Given username starts with a digit that is prohibited.");
    public static Error UsernameStartsWithUnderscore => new("Username.StartsWithUnderscore", "Given username starts with an underscore that is prohibited.");
    public static Error UsernameEndsWithUnderscore => new("Username.EndsWithUnderscore", "Given username ends with an underscore that is prohibited.");
    public static Error UsernameProhibitedSymbols => new("Username.ProhibitedSymbols", "Given username contains prohibited symbols.");

    // Password Errors
    public static Error PasswordTooShort => new("Password.TooShort", $"Given password length is less than {Password.MinLength} symbols.");
    public static Error PasswordHasNoLowercase => new("Password.HasNoLowercase", "Given password does not contain any lowercase letter.");
    public static Error PasswordHasNoUppercase => new("Password.HasNoUppercase", "Given password does not contain any uppercase letter.");
    public static Error PasswordHasNoDigit => new("Password.HasNoDigit", "Given password does not contain any digit.");

}