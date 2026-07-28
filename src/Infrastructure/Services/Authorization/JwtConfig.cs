using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services.Authorization;

public sealed record JwtConfig
{
    public required string Audience { get; init; }
    public required string Issuer { get; init; }
    public required TimeSpan Expiration { get; init; }
    public required string Secret { get; init; }
    public required string SecurityAlgorithm { get; init; }

    public SymmetricSecurityKey SymmetricSecurityKey => new(Encoding.UTF8.GetBytes(Secret));
}