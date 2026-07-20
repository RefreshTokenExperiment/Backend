using Shared;

namespace Domain.Users;

public sealed record Username
{
    public string Value { get; init; }

    public const int MaxLength = 16;

    private static readonly HashSet<char> AllowedChars = [..GetAllowedChars()];
    private static IEnumerable<char> GetAllowedChars()
    {
        yield return '$';
        yield return '_';

        // Numbers 0-9
        foreach (var number in Enumerable.Range(48, 10).Select(x => (char)x))
            yield return number;
        
        // Letters A-Z
        foreach (var letter in Enumerable.Range(65, 26).Select(x => (char)x))
            yield return letter;

        // Letters a-z
        foreach (var letter in Enumerable.Range(97, 26).Select(x => (char)x))
            yield return letter;
    }

    private Username(string value)
    {
        Value = value;
    }

    public static Result<Username> Create(string username)
    {
        var validationError = Validate(username);
        return validationError is null ? new Username(username) : validationError;
    }

    private static Error? Validate(string username)
    {
        if (username.Length > MaxLength) return UserErrors.UsernameIsTooLong;
        if (char.IsDigit(username[0])) return UserErrors.UsernameStartsWithDigit;
        if (username.StartsWith('_')) return UserErrors.UsernameStartsWithUnderscore;
        if (username.EndsWith('_')) return UserErrors.UsernameEndsWithUnderscore;
        if (username.Any(ch => !AllowedChars.Contains(ch))) return UserErrors.UsernameProhibitedSymbols;

        return null;
    }

    public static implicit operator string(Username username)
        => username.Value;
}