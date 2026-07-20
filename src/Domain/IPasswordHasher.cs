namespace Domain;

public interface IPasswordHasher
{
    public string Hash(string password);
}