namespace Application.Auth.Commands;

public sealed record AuthResult
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
}