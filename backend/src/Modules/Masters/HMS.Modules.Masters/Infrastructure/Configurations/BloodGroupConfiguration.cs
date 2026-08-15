using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

internal class BloodGroupConfiguration : IEntityTypeConfiguration<BloodGroup>
{
    private static readonly DateTime SeedCreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<BloodGroup> builder)
    {
        builder.ToTable("blood_groups");

        builder.HasKey(b => b.Id).HasName("pk_blood_groups");
        builder.Property(b => b.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(b => b.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(b => b.Name).HasColumnName("name").HasMaxLength(10).IsRequired();
        builder.Property(b => b.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.Property(b => b.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(b => b.CreatedBy).HasColumnName("created_by");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");
        builder.Property(b => b.UpdatedBy).HasColumnName("updated_by");
        builder.Property(b => b.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(b => b.DeletedAt).HasColumnName("deleted_at");
        builder.Property(b => b.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(b => !b.IsDeleted);

        builder.HasIndex(b => b.Code).IsUnique().HasDatabaseName("ux_blood_groups_code").HasFilter("is_deleted = false");

        // Standalone future-use seed data — every tenant database gets these eight rows
        // automatically the moment this migration applies. Not referenced by Patient or
        // any other entity in this branch (see docs/DecisionLog.md's SaaS provisioning ADR).
        builder.HasData(
            Seed("019a0000-0000-7000-8000-000000000011", "A_POS", "A+"),
            Seed("019a0000-0000-7000-8000-000000000012", "A_NEG", "A-"),
            Seed("019a0000-0000-7000-8000-000000000013", "B_POS", "B+"),
            Seed("019a0000-0000-7000-8000-000000000014", "B_NEG", "B-"),
            Seed("019a0000-0000-7000-8000-000000000015", "O_POS", "O+"),
            Seed("019a0000-0000-7000-8000-000000000016", "O_NEG", "O-"),
            Seed("019a0000-0000-7000-8000-000000000017", "AB_POS", "AB+"),
            Seed("019a0000-0000-7000-8000-000000000018", "AB_NEG", "AB-"));
    }

    private static object Seed(string id, string code, string name) => new
    {
        Id = Guid.Parse(id),
        Code = code,
        Name = name,
        IsActive = true,
        CreatedAt = SeedCreatedAt,
        CreatedBy = (Guid?)null,
        UpdatedAt = (DateTime?)null,
        UpdatedBy = (Guid?)null,
        IsDeleted = false,
        DeletedAt = (DateTime?)null,
        DeletedBy = (Guid?)null,
    };
}
