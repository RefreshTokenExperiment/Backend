using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services.Authorization;

public interface IJwtConfig
{
    public string Audience { get; }
    public string Issuer { get; }
    public TimeSpan Expiration { get; }
    public string Secret { get; }
    public string SecurityAlgorithm { get; }

    public SymmetricSecurityKey SymmetricSecurityKey => new(Encoding.UTF8.GetBytes(Secret));
}