namespace Application.Abstractions;

public interface IAccessTokenBlacklist
{
    public Task<bool> ExistsAsync(string accessToken, CancellationToken cancellationToken = default);
    public Task BlockAsync(string accessToken, CancellationToken cancellationToken = default);
}