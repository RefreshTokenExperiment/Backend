using Domain.Users;

namespace Domain.RefreshTokens;

public sealed class RefreshToken
{
    // Primary Hash of Refresh Token
    public string Hash { get; set; } = null!;
    public User User { get; set; } = null!;
    public string DeviceId { get; set; } = null!;
    public DateTimeOffset LastTimeUsed { get; set; }
    public bool IsRevoked { get; set; }

    public void Revoke() => IsRevoked = true;

    #pragma warning disable IDE0290
    public RefreshToken(string hash, User user, string deviceId)
    {
        Hash = hash;
        User = user;
        DeviceId = deviceId;

        LastTimeUsed = DateTimeOffset.UtcNow;
    }
    #pragma warning restore IDE0290

    // Used for Entity Framework Core
    private RefreshToken() { }
}