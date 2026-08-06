using FluentAssertions;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Domain;

public class PatientRegistrationTests
{
    private static readonly Guid PatientId = Guid.NewGuid();

    [Fact]
    public void Create_SetsFieldsAndCreatedAudit()
    {
        var actorId = Guid.NewGuid();

        var registration = PatientRegistration.Create(
            PatientId, "OP-2026-000001", EncounterType.OP, ModeOfArrival.WalkIn,
            "General Medicine", "Dr. Smith", null, "Self", "General", actorId);

        registration.PatientId.Should().Be(PatientId);
        registration.RegistrationNumber.Should().Be("OP-2026-000001");
        registration.EncounterType.Should().Be(EncounterType.OP);
        registration.ModeOfArrival.Should().Be(ModeOfArrival.WalkIn);
        registration.Department.Should().Be("General Medicine");
        registration.Consultant.Should().Be("Dr. Smith");
        registration.AdmissionType.Should().BeNull();
        registration.ReferralSource.Should().Be("Self");
        registration.Category.Should().Be("General");
        registration.CreatedBy.Should().Be(actorId);
    }

    [Fact]
    public void Create_TrimsDepartmentAndConsultant()
    {
        var registration = PatientRegistration.Create(
            PatientId, "IP-2026-000002", EncounterType.IP, ModeOfArrival.Ambulance,
            "  ICU  ", "  Dr. Lee  ", AdmissionType.MLC, null, null, null);

        registration.Department.Should().Be("ICU");
        registration.Consultant.Should().Be("Dr. Lee");
    }

    [Fact]
    public void Create_WithNullOrWhitespaceDepartment_Throws()
    {
        var act = () => PatientRegistration.Create(
            PatientId, "OP-2026-000003", EncounterType.OP, ModeOfArrival.WalkIn,
            "   ", "Dr. Smith", null, null, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullOrWhitespaceConsultant_Throws()
    {
        var act = () => PatientRegistration.Create(
            PatientId, "OP-2026-000004", EncounterType.OP, ModeOfArrival.WalkIn,
            "General Medicine", "   ", null, null, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullOrWhitespaceRegistrationNumber_Throws()
    {
        var act = () => PatientRegistration.Create(
            PatientId, "   ", EncounterType.OP, ModeOfArrival.WalkIn,
            "General Medicine", "Dr. Smith", null, null, null, null);

        act.Should().Throw<ArgumentException>();
    }
}
