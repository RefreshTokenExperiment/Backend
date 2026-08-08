namespace WebAPI.Endpoints.Auth;

public static partial class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder builder, string route = "/api/auth")
    {
        var api = builder.MapGroup(route);
        
        api.MapPost("/login", Login);
        api.MapPost("/signup", SignUp);
        api.MapPost("/refresh", Refresh);
        api.MapPost("/logout", Logout).RequireAuthorization();
    }
}