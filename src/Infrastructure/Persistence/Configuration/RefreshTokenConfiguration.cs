using Domain.RefreshTokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configuration;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(x => x.Hash);
        builder.HasOne(x => x.User);

        builder
            .Property(x => x.Hash)
            .HasColumnName("Hash")
            .IsRequired();

        builder
            .Property(x => x.DeviceId)
            .HasColumnName("DeviceId")
            .IsRequired();

        builder
            .Property(x => x.LastTimeUsed)
            .HasColumnName("LastTimeUsed")
            .IsRequired();

        builder
            .Property(x => x.IsRevoked)
            .HasColumnName("IsRevoked")
            .HasDefaultValue(false)
            .IsRequired();
    }
}