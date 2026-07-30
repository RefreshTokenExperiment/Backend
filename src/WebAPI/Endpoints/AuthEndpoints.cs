namespace WebAPI.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder builder, string route = "/api/auth")
    {
        var api = builder.MapGroup(route);
        
        api.MapPost("/login", () => "Login Page");
        api.MapPost("/signup", () => "Sign-Up Page");
        api.MapPost("/refresh", () => "Refresh Access Page");
        api.MapPost("/logout", () => "Logout Page");
    }
}