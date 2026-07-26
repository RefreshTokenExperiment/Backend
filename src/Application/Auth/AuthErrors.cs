using Shared;

namespace Application.Auth;

public record AuthErrors(string Code, string Message) : Error(Code, Message)
{
    public sealed record UserAlreadyExists() : AuthErrors("User.AlreadyExists", "User with such Email or Username already exists.");
    public sealed record DeviceIdIsNull() : AuthErrors("DeviceId.IsNull", "User's Device ID cannot be empty.");
    public sealed record InvalidCredentials() : AuthErrors("Credentials.Invalid", "Given credentials are invalid.");
}