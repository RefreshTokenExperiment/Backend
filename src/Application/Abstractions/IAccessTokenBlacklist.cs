namespace Application.Abstractions;

public interface IAccessTokenBlacklist
{
    public Task<bool> Exists(string accessToken);
    public Task Block(string accessToken);
}