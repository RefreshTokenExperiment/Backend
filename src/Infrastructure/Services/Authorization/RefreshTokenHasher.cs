using System.Security.Cryptography;
using System.Text;
using Application.Abstractions;

namespace Infrastructure.Services.Authorization;

public sealed class RefreshTokenHasher : IRefreshTokenHasher
{
    public string Hash(string refreshToken)
    {
        var hashBytes = SHA512.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexStringLower(hashBytes);
    }
}