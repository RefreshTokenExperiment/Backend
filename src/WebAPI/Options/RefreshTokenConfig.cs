using Application.Abstractions;

namespace WebAPI.Options;

public sealed class RefreshTokenConfig : IRefreshTokenConfig
{
    public const string Path = nameof(RefreshTokenConfig);

    public TimeSpan MaxLifetime { get; init; }

    public TimeSpan MaxInactivity { get; init; }
}