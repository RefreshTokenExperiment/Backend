using WebAPI.Constants;

namespace WebAPI.Extensions;

public static class HttpContextExtensions
{
    private static readonly CookieOptions _cookieOptions = new()
    {
        Expires = DateTimeOffset.UtcNow.Add(TimeSpan.FromDays(180)),
        HttpOnly = true,
        SameSite = SameSiteMode.Strict
    };

    internal static string GetOrSetDeviceId(this HttpContext context)
    {
        var cookieName = CookieNameConstants.DeviceId;
        var foundCookie = context.Request.Cookies.SingleOrDefault(x => x.Key == cookieName);
        if (foundCookie.Key != default) return foundCookie.Value;

        var deviceId = Guid.CreateVersion7().ToString();
        context.Response.Cookies.Append(cookieName, deviceId, _cookieOptions);
        return deviceId;
    }

    internal static void SetRefreshTokenCookie(this HttpContext context, string refreshToken)
    {
        context.Response.Cookies.Append(CookieNameConstants.Refresh, refreshToken, _cookieOptions);
    }

    internal static string? GetRefreshTokenCookie(this HttpContext context)
    {
        return context.Request.Cookies[CookieNameConstants.Refresh];
    }

    internal static string? PopRefreshTokenCookie(this HttpContext context)
    {
        var refreshToken = context.Request.Cookies[CookieNameConstants.Refresh];
        context.Response.Cookies.Delete(CookieNameConstants.Refresh);
        return refreshToken;
    }

    internal static string? GetAccessToken(this HttpContext context)
    {
        var accessToken = context.Request.Headers.Authorization;
        if (accessToken == string.Empty) return null;

        return accessToken.ToString()["Bearer ".Length..];
    }
}