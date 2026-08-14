using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

internal class GenderConfiguration : IEntityTypeConfiguration<Gender>
{
    private static readonly DateTime SeedCreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Gender> builder)
    {
        builder.ToTable("genders");

        builder.HasKey(g => g.Id).HasName("pk_genders");
        builder.Property(g => g.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(g => g.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(g => g.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        builder.Property(g => g.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(g => g.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(g => g.CreatedBy).HasColumnName("created_by");
        builder.Property(g => g.UpdatedAt).HasColumnName("updated_at");
        builder.Property(g => g.UpdatedBy).HasColumnName("updated_by");
        builder.Property(g => g.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(g => g.DeletedAt).HasColumnName("deleted_at");
        builder.Property(g => g.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(g => !g.IsDeleted);

        builder.HasIndex(g => g.Code).IsUnique().HasDatabaseName("ux_genders_code").HasFilter("is_deleted = false");

        // Standalone future-use seed data — every tenant database gets these three rows
        // automatically the moment this migration applies. Not referenced by Patient or
        // any other entity in this branch (see docs/DecisionLog.md's SaaS provisioning ADR).
        builder.HasData(
            new
            {
                Id = Guid.Parse("019a0000-0000-7000-8000-000000000001"),
                Code = "MALE",
                Name = "Male",
                IsActive = true,
                CreatedAt = SeedCreatedAt,
                CreatedBy = (Guid?)null,
                UpdatedAt = (DateTime?)null,
                UpdatedBy = (Guid?)null,
                IsDeleted = false,
                DeletedAt = (DateTime?)null,
                DeletedBy = (Guid?)null,
            },
            new
            {
                Id = Guid.Parse("019a0000-0000-7000-8000-000000000002"),
                Code = "FEMALE",
                Name = "Female",
                IsActive = true,
                CreatedAt = SeedCreatedAt,
                CreatedBy = (Guid?)null,
                UpdatedAt = (DateTime?)null,
                UpdatedBy = (Guid?)null,
                IsDeleted = false,
                DeletedAt = (DateTime?)null,
                DeletedBy = (Guid?)null,
            },
            new
            {
                Id = Guid.Parse("019a0000-0000-7000-8000-000000000003"),
                Code = "OTHER",
                Name = "Other",
                IsActive = true,
                CreatedAt = SeedCreatedAt,
                CreatedBy = (Guid?)null,
                UpdatedAt = (DateTime?)null,
                UpdatedBy = (Guid?)null,
                IsDeleted = false,
                DeletedAt = (DateTime?)null,
                DeletedBy = (Guid?)null,
            });
    }
}
