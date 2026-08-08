using System.Security.Claims;
using Application.Auth.Commands.Logout;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Shared;
using WebAPI.Extensions;

namespace WebAPI.Endpoints.Auth;

public static partial class AuthEndpoints
{
    private static async Task<Results<Ok, InternalServerError<Error>>> Logout(
        [FromServices] LogoutCommandHandler handler,
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        var accessToken = context.GetAccessToken();
        var refreshToken = context.PopRefreshTokenCookie();
        var claim = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var command = new LogoutCommand(accessToken!, refreshToken, claim);
        var result = await handler.HandleAsync(command, cancellationToken);

        return !result.IsFailure
        ? TypedResults.Ok()
        : TypedResults.InternalServerError(result.Error);
    }
}