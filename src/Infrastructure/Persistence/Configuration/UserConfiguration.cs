using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configuration;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);
        builder
            .Property(x => x.Id)
            .HasConversion(userId => userId.Value, id => new UserId(id))
            .HasColumnName("Id")
            .IsRequired();

        builder
            .Property(x => x.Email)
            .HasConversion(emailVO => emailVO.Value, email => Email.Create(email).Value!)
            .HasColumnName("Email")
            .IsRequired();

        builder
            .Property(x => x.Username)
            .HasConversion(usernameVO => usernameVO.Value, username => Username.Create(username).Value!)
            .HasColumnName("Username")
            .IsRequired();

        builder
            .Property(x => x.Password)
            .HasConversion(passwordVO => passwordVO.Value, password => Password.FromHash(password))
            .HasColumnName("Password")
            .IsRequired();
    }
}