using System.Security.Claims;
using System.Security.Cryptography;
using Application.Abstractions;
using Domain.Users;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services.Authorization;

public sealed class JwtGenerator(IJwtConfig config, RefreshTokenHasher hasher) : IAuthorizationTokenGenerator
{
    public string GenerateAccess(User user)
    {
        var claims = new Claim[]
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString()),
            new(JwtRegisteredClaimNames.Nickname, user.Username.Value)
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Expires = DateTimeOffset.Now.Add(config.Expiration).UtcDateTime,
            Audience = config.Audience,
            Issuer = config.Issuer,
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(config.SymmetricSecurityKey, config.SecurityAlgorithm)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public (string Token, string Hash) GenerateRefresh()
    {
        var token = RandomNumberGenerator.GetHexString(32);
        var hash = hasher.Hash(token);
        return (token, hash);
    }
}