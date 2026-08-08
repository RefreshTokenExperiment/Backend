using Shared;
using Application.Auth;
using Application.Auth.Commands.Login;
using WebAPI.Extensions;
using WebAPI.RequestDto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WebAPI.Endpoints.Auth;

public static partial class AuthEndpoints
{
    private static async Task<Results<Ok<string>, ForbidHttpResult, InternalServerError<Error>>> Login(
        [FromBody] LoginDto dto,
        [FromServices] LoginCommandHandler handler,
        HttpContext context,
        CancellationToken cancellationToken = default
    )
    {
        var deviceId = context.GetOrSetDeviceId();
        var command = new LoginCommand(dto.Login, dto.Password, deviceId);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsFailure)
        {
            var (refresh, access) = (result.Value.RefreshToken, result.Value.AccessToken);
            context.SetRefreshTokenCookie(refresh);
            return TypedResults.Ok(access);
        }
        
        return result.Error switch
        {
            AuthErrors.InvalidCredentials => TypedResults.Forbid(),
            _ => TypedResults.InternalServerError(result.Error)
        };
    }
}