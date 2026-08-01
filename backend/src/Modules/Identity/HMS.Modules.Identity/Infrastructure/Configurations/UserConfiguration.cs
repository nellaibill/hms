using HMS.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Identity.Infrastructure.Configurations;

/// <summary>
/// Maps <see cref="User"/> to identity.users, following the naming/PK/audit-column/
/// soft-delete standards in docs/DatabaseArchitecture.md.
/// Internal (not public): <see cref="User"/> is an internal domain type, so a public
/// Configure(EntityTypeBuilder&lt;User&gt;) member would be a CS0051 accessibility violation.
/// EF Core's <c>ApplyConfigurationsFromAssembly</c> discovers and invokes this via
/// reflection regardless of the type's own visibility, so internal is sufficient here.
/// </summary>
internal class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id).HasName("pk_users");
        builder.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // Generated in the domain (User.Create), not by the database.

        builder.Property(u => u.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
        builder.Property(u => u.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
        builder.Property(u => u.PhoneNumber).HasColumnName("phone_number").HasMaxLength(30);
        builder.Property(u => u.ProfilePhotoUrl).HasColumnName("profile_photo_url").HasMaxLength(500);

        builder.Property(u => u.RoleId).HasColumnName("role_id").IsRequired();

        builder.HasOne(u => u.Role)
            .WithMany()
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict); // A role in use by a user cannot be deleted out from under them.

        // Additive credential-support columns per ADR-001 (docs/DecisionLog.md) — see the
        // doc comment on User itself. PasswordHash is nullable/unset until Authentication
        // ships; no column here stores a plaintext password.
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
        builder.Property(u => u.EmailVerified).HasColumnName("email_verified").IsRequired().HasDefaultValue(false);
        builder.Property(u => u.LastLoginAt).HasColumnName("last_login_at");

        builder.Property(u => u.IsActive).HasColumnName("is_active").IsRequired();

        // Standard audit columns (docs/DatabaseArchitecture.md §5).
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(u => u.CreatedBy).HasColumnName("created_by");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
        builder.Property(u => u.UpdatedBy).HasColumnName("updated_by");
        builder.Property(u => u.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(u => u.DeletedAt).HasColumnName("deleted_at");
        builder.Property(u => u.DeletedBy).HasColumnName("deleted_by");

        // Optimistic concurrency via Postgres's own system column — no extra column needed
        // (docs/DatabaseArchitecture.md §5's row_version guidance). The Npgsql provider's
        // former UseXminAsConcurrencyToken() convenience method no longer exists in the
        // referenced 10.0.0 package, so this is the equivalent, still-supported manual
        // mapping of the "xmin" system column as a row-version concurrency token.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .IsRowVersion();

        // Soft-deleted users are excluded from every query unless explicitly requested
        // (docs/DatabaseArchitecture.md §6).
        builder.HasQueryFilter(u => !u.IsDeleted);

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("ux_users_email")
            .HasFilter("is_deleted = false");

        builder.HasIndex(u => u.Username)
            .IsUnique()
            .HasDatabaseName("ux_users_username")
            .HasFilter("is_deleted = false");

        builder.HasIndex(u => u.IsActive).HasDatabaseName("ix_users_is_active");

        builder.HasIndex(u => u.RoleId).HasDatabaseName("ix_users_role_id");
    }
}
