using Application.Abstractions;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Application.Auth.Commands.Logout;

public sealed class LogoutCommandHandler(IRefreshTokenHasher hasher, IDbContext db, IAccessTokenBlacklist tokenBlacklist)
{
    public async Task<Result> HandleAsync(LogoutCommand command, CancellationToken cancellationToken = default)
    {
        // Validate UserId
        if (!Guid.TryParse(command.UserId, out var userId)) return new AuthErrors.InvalidCredentials();

        // Blacklist Access Token.
        await tokenBlacklist.BlockAsync(command.AccessToken, cancellationToken);
        
        // Find Refresh Token and Revoke it.
        if (command.RefreshToken is not { } refreshToken) return Result.Success();
        var hash = hasher.Hash(refreshToken);

        var foundRefreshToken = await db.RefreshTokens.SingleOrDefaultAsync(
            x => x.Hash == hash 
            && x.User.Id == new UserId(userId), cancellationToken);
        foundRefreshToken?.Revoke();
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

