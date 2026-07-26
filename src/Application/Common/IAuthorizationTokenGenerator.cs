using Domain.Users;

namespace Application.Common;

public interface IAuthorizationTokenGenerator
{
    public string GenerateAccess(User user);
    public (string Token, string Hash) GenerateRefresh();
}