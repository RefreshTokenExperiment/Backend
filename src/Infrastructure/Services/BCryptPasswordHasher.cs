using Application.Abstractions;
using Domain.Users;

namespace Infrastructure.Services;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password);
    }

    public bool Verify(string password, Password originalPassword)
    {
        return BCrypt.Net.BCrypt.EnhancedVerify(password, originalPassword);
    }
}