using HMS.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Patients.Infrastructure.Configurations;

/// <summary>
/// Maps <see cref="Address"/> to patients.addresses. PatientId is both the primary key and
/// the foreign key back to Patient — there is no separate surrogate address_id, enforcing a
/// true 1:1 at the database level (a patient can never have more than one address row).
/// </summary>
internal class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("addresses");

        builder.HasKey(a => a.PatientId).HasName("pk_addresses");
        builder.Property(a => a.PatientId).HasColumnName("patient_id").ValueGeneratedNever();

        builder.Property(a => a.AddressLine1).HasColumnName("address_line_1").HasMaxLength(200).IsRequired();
        builder.Property(a => a.AddressLine2).HasColumnName("address_line_2").HasMaxLength(200);
        builder.Property(a => a.AddressLine3).HasColumnName("address_line_3").HasMaxLength(200);
        builder.Property(a => a.StateId).HasColumnName("state_id").IsRequired();
        builder.Property(a => a.DistrictId).HasColumnName("district_id").IsRequired();
        builder.Property(a => a.Pincode).HasColumnName("pincode").HasMaxLength(6).IsRequired();
    }
}
