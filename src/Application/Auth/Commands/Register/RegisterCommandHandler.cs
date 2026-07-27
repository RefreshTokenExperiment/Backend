using Shared;
using Domain.Users;
using Domain.RefreshTokens;
using Microsoft.EntityFrameworkCore;
using Application.Abstractions;
using Application.Common;

namespace Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    IDbContext context,
    PasswordFactory passwordFactory,
    IAuthorizationTokenGenerator tokenGenerator)
{
    public async Task<Result<AuthResult>> HandleAsync(RegisterCommand command, CancellationToken cancellationToken = default)
    {
        // === Validating User's Credentials ===
        var email = Email.Create(command.Email);
        if (email.IsFailure) return email.Error;

        var username = Username.Create(command.Username);
        if (username.IsFailure) return username.Error;

        if (string.IsNullOrWhiteSpace(command.DeviceId)) return new AuthErrors.DeviceIdIsNull();

        // var password = Password.Create(command.Password, hasher);
        var password = passwordFactory.Create(command.Password);
        if (password.IsFailure) return password.Error;


        // === Check if there is already a User with such Email or Username ===
        var exists = await context.Users.AsNoTracking().AnyAsync(user => user.Email == email.Value || user.Username == username.Value, cancellationToken);
        if (exists) return new AuthErrors.UserAlreadyExists();

        
        // === Creating the User and Saving it to Database ===
        var user = new User(email, username, password);
        await context.Users.AddAsync(user, cancellationToken);


        // === Revoke Refresh that used on the Device and Save new Refresh ===
        var tokenFoundOnDevice = await context.RefreshTokens.SingleOrDefaultAsync(token => token.DeviceId == command.DeviceId, cancellationToken);
        tokenFoundOnDevice?.Revoke();


        // === Generating Authorization Tokens ===
        var access = tokenGenerator.GenerateAccess(user);
        var (token, hash) = tokenGenerator.GenerateRefresh();

        var refreshToken = new RefreshToken(
            hash: hash,
            user: user,
            deviceId: command.DeviceId);

        await context.RefreshTokens.AddAsync(refreshToken, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException updateException)
        {
            if (updateException.InnerException is not Npgsql.PostgresException postgresException) throw;
            if (postgresException.SqlState != Npgsql.PostgresErrorCodes.UniqueViolation) throw;
            return new AuthErrors.UserAlreadyExists();
        }

        return new AuthResult
        {
            AccessToken = access,
            RefreshToken = token
        };
    }
}