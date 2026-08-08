namespace WebAPI.RequestDto;

public sealed record LoginDto(
    string Login,
    string Password
);