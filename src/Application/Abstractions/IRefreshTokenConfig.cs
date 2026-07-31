namespace Application.Abstractions;

public interface IRefreshTokenConfig
{
    public TimeSpan MaxLifetime { get; }
    public TimeSpan MaxInactivity { get; }
}