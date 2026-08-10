using FluentAssertions;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Domain;

public class PatientRegistrationTests
{
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid DepartmentId = Guid.NewGuid();
    private static readonly Guid ConsultantId = Guid.NewGuid();

    [Fact]
    public void Create_SetsFieldsAndCreatedAudit()
    {
        var actorId = Guid.NewGuid();

        var registration = PatientRegistration.Create(
            PatientId, "OP-2026-000001", EncounterType.OP, ModeOfArrival.WalkIn,
            DepartmentId, ConsultantId, null, "Self", "General", actorId);

        registration.PatientId.Should().Be(PatientId);
        registration.RegistrationNumber.Should().Be("OP-2026-000001");
        registration.EncounterType.Should().Be(EncounterType.OP);
        registration.ModeOfArrival.Should().Be(ModeOfArrival.WalkIn);
        registration.DepartmentId.Should().Be(DepartmentId);
        registration.ConsultantId.Should().Be(ConsultantId);
        registration.AdmissionType.Should().BeNull();
        registration.ReferralSource.Should().Be("Self");
        registration.Category.Should().Be("General");
        registration.CreatedBy.Should().Be(actorId);
    }

    [Fact]
    public void Create_WithNullOrWhitespaceRegistrationNumber_Throws()
    {
        var act = () => PatientRegistration.Create(
            PatientId, "   ", EncounterType.OP, ModeOfArrival.WalkIn,
            DepartmentId, ConsultantId, null, null, null, null);

        act.Should().Throw<ArgumentException>();
    }
}
