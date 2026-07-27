using Application.Abstractions;
using Domain.RefreshTokens;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests.Auth;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    Task IDbContext.SaveChangesAsync(CancellationToken token)
    {
        return SaveChangesAsync(token);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        #region User Entity
        var userBuilder = modelBuilder.Entity<User>();

        userBuilder.ToTable("Users");
        userBuilder.HasKey(x => x.Id);
        userBuilder
            .Property(x => x.Id)
            .HasConversion(userId => userId.Value, id => new UserId(id))
            .HasColumnName("Id")
            .IsRequired();

        userBuilder
            .Property(x => x.Email)
            .HasConversion(emailVO => emailVO.Value, email => Email.Create(email).Value!)
            .HasColumnName("Email")
            .IsRequired();

        userBuilder
            .Property(x => x.Username)
            .HasConversion(usernameVO => usernameVO.Value, username => Username.Create(username).Value!)
            .HasColumnName("Username")
            .IsRequired();

        userBuilder
            .Property(x => x.Password)
            .HasConversion(passwordVO => passwordVO.Value, password => Password.FromHash(password))
            .HasColumnName("Password")
            .IsRequired();
        #endregion
    
        #region Refresh Token Entity
        var tokenBuilder = modelBuilder.Entity<RefreshToken>();

        tokenBuilder.HasKey(x => x.Hash);
        tokenBuilder.HasOne(x => x.User);

        tokenBuilder
            .Property(x => x.Hash)
            .HasColumnName("Hash")
            .IsRequired();

        tokenBuilder
            .Property(x => x.DeviceId)
            .HasColumnName("DeviceId")
            .IsRequired();

        tokenBuilder
            .Property(x => x.LastTimeUsed)
            .HasColumnName("LastTimeUsed")
            .IsRequired();

        tokenBuilder
            .Property(x => x.IsRevoked)
            .HasColumnName("IsRevoked")
            .HasDefaultValue(false)
            .IsRequired();
        #endregion
    }
}