using Domain.Users;

namespace Application.Abstractions;

public interface IPasswordHasher
{
    public string Hash(string password);
    public bool Verify(string password, Password originalPassword);
}