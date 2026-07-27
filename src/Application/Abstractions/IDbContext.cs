using Domain.Users;
using Domain.RefreshTokens;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions;

public interface IDbContext
{
    public DbSet<User> Users { get; }
    public DbSet<RefreshToken> RefreshTokens { get; }

    public Task SaveChangesAsync(CancellationToken token = default);
}