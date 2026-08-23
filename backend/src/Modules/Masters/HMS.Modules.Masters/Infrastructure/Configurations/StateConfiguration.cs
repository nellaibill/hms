using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

/// <summary>
/// Maps <see cref="State"/> to masters.states and seeds India's 28 states + 8 union
/// territories — the only country this app supports (see docs/DecisionLog.md), so states
/// are the top level with no Country table above them. Read-only reference data: no admin
/// CRUD in this iteration, same "standalone seeded lookup" shape as Gender/BloodGroup.
/// </summary>
internal class StateConfiguration : IEntityTypeConfiguration<State>
{
    private static readonly DateTime SeedCreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Name + deterministic id for every seeded state, in India's standard states-then-UTs
    /// listing order. Exposed (not private) so DistrictConfiguration's seed can resolve a
    /// district's parent state id by name without a second migration pass.
    /// </summary>
    internal static readonly IReadOnlyList<(string Name, Guid Id)> Seed = BuildSeed();

    private static IReadOnlyList<(string Name, Guid Id)> BuildSeed()
    {
        string[] names =
        [
            // 28 states
            "Andhra Pradesh", "Arunachal Pradesh", "Assam", "Bihar", "Chhattisgarh", "Goa",
            "Gujarat", "Haryana", "Himachal Pradesh", "Jharkhand", "Karnataka", "Kerala",
            "Madhya Pradesh", "Maharashtra", "Manipur", "Meghalaya", "Mizoram", "Nagaland",
            "Odisha", "Punjab", "Rajasthan", "Sikkim", "Tamil Nadu", "Telangana", "Tripura",
            "Uttar Pradesh", "Uttarakhand", "West Bengal",
            // 8 union territories
            "Andaman and Nicobar Islands", "Chandigarh", "Dadra and Nagar Haveli and Daman and Diu",
            "Delhi", "Jammu and Kashmir", "Ladakh", "Lakshadweep", "Puducherry",
        ];

        return names.Select((name, index) => (name, StateId(index + 1))).ToList();
    }

    /// <summary>Deterministic id generator shared with the seed migration — index is the
    /// state's 1-based position in <see cref="BuildSeed"/>'s name list, stable across
    /// regenerations since that list is only ever appended to, never reordered.</summary>
    internal static Guid StateId(int index) => Guid.Parse($"019a0100-0000-7000-8000-{index:D12}");

    public void Configure(EntityTypeBuilder<State> builder)
    {
        builder.ToTable("states");

        builder.HasKey(s => s.Id).HasName("pk_states");
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(100).IsRequired();

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnName("created_by");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");
        builder.Property(s => s.UpdatedBy).HasColumnName("updated_by");
        builder.Property(s => s.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(s => s.DeletedAt).HasColumnName("deleted_at");
        builder.Property(s => s.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.HasIndex(s => s.Name).IsUnique().HasDatabaseName("ux_states_name").HasFilter("is_deleted = false");

        builder.HasData(Seed.Select(s => new
        {
            Id = s.Id,
            Name = s.Name,
            CreatedAt = SeedCreatedAt,
            CreatedBy = (Guid?)null,
            UpdatedAt = (DateTime?)null,
            UpdatedBy = (Guid?)null,
            IsDeleted = false,
            DeletedAt = (DateTime?)null,
            DeletedBy = (Guid?)null,
        }));
    }
}
