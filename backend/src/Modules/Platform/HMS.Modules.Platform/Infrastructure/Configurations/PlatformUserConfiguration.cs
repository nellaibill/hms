using HMS.Modules.Platform.Contracts;
using HMS.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Platform.Infrastructure.Configurations;

internal class PlatformUserConfiguration : IEntityTypeConfiguration<PlatformUser>
{
    public void Configure(EntityTypeBuilder<PlatformUser> builder)
    {
        builder.ToTable("platform_users");

        builder.HasKey(u => u.Id).HasName("pk_platform_users");
        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(u => u.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(200).IsRequired();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
        builder.Property(u => u.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);
        builder.Property(u => u.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(20).IsRequired().HasDefaultValue(PlatformRole.SuperAdmin);
        builder.Property(u => u.FailedLoginAttempts).HasColumnName("failed_login_attempts").IsRequired().HasDefaultValue(0);
        builder.Property(u => u.LockedOutUntil).HasColumnName("locked_out_until");

        builder.Property(u => u.MfaSecret).HasColumnName("mfa_secret");
        builder.Property(u => u.MfaEnabled).HasColumnName("mfa_enabled").IsRequired().HasDefaultValue(false);

        builder.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(u => u.CreatedBy).HasColumnName("created_by");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
        builder.Property(u => u.UpdatedBy).HasColumnName("updated_by");
        builder.Property(u => u.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(u => u.DeletedAt).HasColumnName("deleted_at");
        builder.Property(u => u.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(u => !u.IsDeleted);

        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ux_platform_users_email").HasFilter("is_deleted = false");
    }
}
