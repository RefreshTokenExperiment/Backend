using System.Text;
using Infrastructure.Persistence;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using WebAPI.Endpoints.Auth;
using WebAPI.Extensions;
using WebAPI.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthServices();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

#region Swagger Initialization

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("JwtBearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT here..."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement()
    {
        [new OpenApiSecuritySchemeReference("JwtBearer", document)] = []
    });
});

#endregion

#region Authenication & Authorization
var jwtConfig = builder.Configuration.GetSection(JwtConfig.Path).Get<JwtConfig>() 
    ?? throw new InvalidOperationException("JWT configuration is missing.");
    
builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new()
    {
        ValidateAudience = true,
        ValidateIssuer = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidAudience = jwtConfig.Audience,
        ValidIssuer = jwtConfig.Issuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret)),

        NameClaimType = JwtRegisteredClaimNames.Sub,
    };
});
builder.Services.AddAuthorization();
#endregion

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateOnBuild = true;
    options.ValidateScopes = true; 
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapAuthEndpoints();
app.MapHealthChecks("/healthz");

app.Run();
