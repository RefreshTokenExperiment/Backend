using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services.Authorization;

public sealed class RefreshTokenHasher
{
    public string Hash(string refreshToken)
    {
        var hashBytes = SHA512.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Encoding.UTF8.GetString(hashBytes);
    }
}