using Application.Abstractions;
using Infrastructure.Services.Authorization;
using StackExchange.Redis;

namespace Infrastructure.Persistence.Repositories;

public sealed class RedisAccessTokenBlacklist(ConnectionMultiplexer cache, IJwtConfig config) : IAccessTokenBlacklist
{
    public async Task BlockAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        await cache.GetDatabase().StringSetAsync(accessToken, string.Empty, config.Expiration);
    }

    public async Task<bool> ExistsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        return await cache.GetDatabase().KeyExistsAsync(accessToken);
    }
}