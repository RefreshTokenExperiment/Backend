using Domain.Users;

namespace Application.Abstractions;

public interface IAuthorizationTokenGenerator
{
    public string GenerateAccess(User user);
    public (string Token, string Hash) GenerateRefresh();
}