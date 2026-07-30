namespace Application.Abstractions;

public interface IRefreshTokenHasher
{
    public string Hash(string refreshToken);
}