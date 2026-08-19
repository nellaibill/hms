using HMS.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Platform.Infrastructure.Configurations;

internal class RevokedTokenConfiguration : IEntityTypeConfiguration<RevokedToken>
{
    public void Configure(EntityTypeBuilder<RevokedToken> builder)
    {
        builder.ToTable("revoked_tokens");

        // The jti claim itself is the key — there's nothing else to look up a revoked
        // token by, and it's already globally unique (a GUID, per PlatformJwtTokenGenerator).
        builder.HasKey(t => t.Jti).HasName("pk_revoked_tokens");
        builder.Property(t => t.Jti).HasColumnName("jti").HasMaxLength(36).ValueGeneratedNever();

        builder.Property(t => t.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(t => t.RevokedAt).HasColumnName("revoked_at").IsRequired();
    }
}
