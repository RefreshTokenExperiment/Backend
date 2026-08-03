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

        // Find Refresh Token and Revoke it.
        var hash = hasher.Hash(command.RefreshToken);

        var foundRefreshToken = await db.RefreshTokens.SingleOrDefaultAsync(
            x => x.Hash == hash 
            && x.User.Id == new UserId(userId), cancellationToken);
        foundRefreshToken?.Revoke();
        await db.SaveChangesAsync(cancellationToken);

        // Blacklist Access Token.
        await tokenBlacklist.BlockAsync(command.AccessToken, cancellationToken);
        return Result.Success();
    }
}

