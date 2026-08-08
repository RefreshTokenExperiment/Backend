using Application.Auth;
using Application.Auth.Commands.Register;
using Domain.Users;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shared;
using WebAPI.Extensions;
using WebAPI.RequestDto;

namespace WebAPI.Endpoints.Auth;

public static partial class AuthEndpoints
{
    private static async Task<Results<Ok<string>, BadRequest<Error>, Conflict<Error>, InternalServerError<Error>>> SignUp(
        [FromBody] RegisterDto dto,
        [FromServices] RegisterCommandHandler handler,
        HttpContext context,
        CancellationToken cancellationToken = default
    )
    {
        var deviceId = context.GetOrSetDeviceId();
        var command = new RegisterCommand(dto.Email, dto.Username, dto.Password, deviceId);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsFailure)
        {
            var (accessToken, refreshToken) = (result.Value.AccessToken, result.Value.RefreshToken);
            context.SetRefreshTokenCookie(refreshToken);
            return TypedResults.Ok(accessToken);
        }

        return result.Error switch
        {
            UserDomainErrors => TypedResults.BadRequest(result.Error),
            AuthErrors.UserAlreadyExists => TypedResults.Conflict(result.Error),
            _ => TypedResults.InternalServerError(result.Error)
        };
    }
}