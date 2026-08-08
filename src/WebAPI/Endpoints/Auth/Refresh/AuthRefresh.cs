using Application.Auth.Commands.Refresh;
using WebAPI.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared;
using Application.Auth;

namespace WebAPI.Endpoints.Auth;

public static partial class AuthEndpoints
{
    private static async Task<Results<Ok<string>, ForbidHttpResult, InternalServerError<Error>>> Refresh(
        [FromServices] RefreshCommandHandler handler,
        HttpContext context,
        CancellationToken cancellationToken = default
    )
    {
        var deviceId = context.GetOrSetDeviceId();
        var refreshToken = context.GetRefreshTokenCookie();
        var command = new RefreshCommand(refreshToken, deviceId);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsFailure)

            return TypedResults.Ok(result.Value.AccessToken);
        
        return result.Error switch
        {
            AuthErrors.InvalidCredentials => TypedResults.Forbid(),
            _ => TypedResults.InternalServerError(result.Error)
        };
    }
}