namespace Application.Auth.Commands.Login;

public sealed record LoginCommand(
    string EmailOrUsername,
    string Password,
    string DeviceId
);