using HMS.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Platform.Infrastructure.Configurations;

internal class PlatformMfaChallengeConfiguration : IEntityTypeConfiguration<PlatformMfaChallenge>
{
    public void Configure(EntityTypeBuilder<PlatformMfaChallenge> builder)
    {
        builder.ToTable("platform_mfa_challenges");

        builder.HasKey(c => c.Id).HasName("pk_platform_mfa_challenges");
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.PlatformUserId).HasColumnName("platform_user_id").IsRequired();
        builder.Property(c => c.Token).HasColumnName("token").HasMaxLength(64).IsRequired();
        builder.Property(c => c.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(c => c.ConsumedAt).HasColumnName("consumed_at");

        // The lookup key on every verify attempt — Token itself is already a
        // cryptographically random, effectively-unique value (see
        // PlatformMfaChallengeStore.GenerateToken), so a unique index also guards against
        // the astronomically unlikely case of a collision.
        builder.HasIndex(c => c.Token).IsUnique().HasDatabaseName("ux_platform_mfa_challenges_token");
    }
}
