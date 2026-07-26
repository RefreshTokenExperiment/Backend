namespace Application.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Username,
    string Password,
    string DeviceId
);