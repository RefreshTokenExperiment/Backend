using Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Application.Auth.Commands.Refresh;

public sealed class RefreshCommandHandler(
    IRefreshTokenHasher hasher,
    IDbContext db,
    IRefreshTokenConfig config,
    IAuthorizationTokenGenerator tokenGenerator)
{
    public async Task<Result<RefreshCommandResult>> HandleAsync(RefreshCommand command, CancellationToken cancellationToken = default)
    {
        // Search Token In Database
        if (command.RefreshToken is null) return new AuthErrors.InvalidCredentials();
        var hash = hasher.Hash(command.RefreshToken);

        var foundToken = await db.RefreshTokens.Include(x => x.User).SingleOrDefaultAsync(x => 
            x.Hash == hash 
            && x.DeviceId == command.DeviceId
            && x.IsRevoked == false, cancellationToken);
        if (foundToken is null) return new AuthErrors.InvalidCredentials();

        // Check if it's lifetime is ended or it's last activity is too far, if true - revoke it
        var isLifetimeEnded = DateTimeOffset.UtcNow - foundToken.CreatedAt > config.MaxLifetime;
        var isTokenInactivated = DateTimeOffset.UtcNow - foundToken.LastTimeUsed > config.MaxInactivity;

        if (isLifetimeEnded || isTokenInactivated)
        {
            foundToken.Revoke();
            await db.SaveChangesAsync(cancellationToken);
            return new AuthErrors.InvalidCredentials();
        }

        var accessToken = tokenGenerator.GenerateAccess(foundToken.User);
        foundToken.LastTimeUsed = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        
        return new RefreshCommandResult(accessToken);
    }
}