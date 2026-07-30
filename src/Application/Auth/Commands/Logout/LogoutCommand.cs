namespace Application.Auth.Commands.Logout;

public sealed record LogoutCommand(
    string AccessToken,
    string RefreshToken,
    string UserId
);