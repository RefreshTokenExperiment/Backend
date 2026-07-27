using System.Linq.Expressions;
using Application.Abstractions;
using Domain.RefreshTokens;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Application.Auth.Commands.Login;

public sealed class LoginCommandHandler(IDbContext context, IPasswordHasher hasher, IAuthorizationTokenGenerator tokenGenerator)
{
    public async Task<Result<AuthResult>> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        // === Validating User's Credentials ===
        var email = Email.Create(command.EmailOrUsername);
        var username = Username.Create(command.EmailOrUsername);

        if (email.IsFailure && username.IsFailure) return new AuthErrors.InvalidCredentials();
        if (string.IsNullOrWhiteSpace(command.DeviceId)) return new AuthErrors.DeviceIdIsNull();


        // === Searching User in Database ===
        Expression<Func<User, bool>> specification = email.IsFailure ? (user => user.Username == username.Value) : (user => user.Email == email.Value);
        var foundUser = await context.Users.SingleOrDefaultAsync(specification, cancellationToken);

        if (foundUser is null) return new AuthErrors.InvalidCredentials();


        // === Verifying Credentials ===
        var isPasswordCorrect = hasher.Verify(command.Password, foundUser.Password);
        if (!isPasswordCorrect) return new AuthErrors.InvalidCredentials();


        // === Revoke Refresh that used on the Device and Save new Refresh ===
        var tokenFoundOnDevice = await context.RefreshTokens.SingleOrDefaultAsync(token => token.DeviceId == command.DeviceId, cancellationToken);
        tokenFoundOnDevice?.Revoke();


        // === Generating Authorization Tokens ===
        var access = tokenGenerator.GenerateAccess(foundUser);
        var (refresh, hash) = tokenGenerator.GenerateRefresh();

        var refreshToken = new RefreshToken(
            hash: hash,
            user: foundUser,
            deviceId: command.DeviceId);

        await context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return new AuthResult
        {
            AccessToken = access,
            RefreshToken = refresh
        };
    }
}