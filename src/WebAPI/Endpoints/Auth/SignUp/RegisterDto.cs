namespace WebAPI.RequestDto;

public sealed record RegisterDto(
    string Email,
    string Username,
    string Password
);