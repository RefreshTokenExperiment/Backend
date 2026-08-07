namespace Application.Auth.Commands.Refresh;

public sealed record RefreshCommand(
    string? RefreshToken, 
    string DeviceId
);