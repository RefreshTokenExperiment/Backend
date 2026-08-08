using Infrastructure.Services.Authorization;

namespace WebAPI.Options;

public sealed class JwtConfig : IJwtConfig
{
    public const string Path = "JwtConfig";

    public string Audience { get; init; } = null!;

    public string Issuer { get; init; } = null!;

    public TimeSpan Expiration { get; init; }

    public string Secret { get; init; } = null!;

    public string SecurityAlgorithm { get; init; } = null!;
}