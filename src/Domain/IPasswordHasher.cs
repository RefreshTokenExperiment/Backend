using Domain.Users;

namespace Domain;

public interface IPasswordHasher
{
    public string Hash(string password);
    public bool Verify(string password, Password originalPassword);
}