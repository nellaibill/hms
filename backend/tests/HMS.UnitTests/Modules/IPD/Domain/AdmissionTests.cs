using FluentAssertions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Domain;

public class AdmissionTests
{
    private static Admission NewAdmission(Guid? patientId = null, Guid? wardId = null, Guid? bedId = null) => Admission.Create(
        "ADM-2026-000001",
        patientId ?? Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        wardId ?? Guid.NewGuid(),
        bedId ?? Guid.NewGuid(),
        new DateTime(2026, 8, 11, 9, 0, 0, DateTimeKind.Utc),
        AdmissionType.Elective,
        "Observation",
        null);

    [Fact]
    public void Create_SetsFieldsAndDefaultsToAdmitted()
    {
        var patientId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var admission = Admission.Create(
            "ADM-2026-000001",
            patientId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTime(2026, 8, 11, 9, 0, 0, DateTimeKind.Utc),
            AdmissionType.Emergency,
            "Chest pain",
            actorId);

        admission.AdmissionNumber.Should().Be("ADM-2026-000001");
        admission.PatientId.Should().Be(patientId);
        admission.AdmissionType.Should().Be(AdmissionType.Emergency);
        admission.ReasonForAdmission.Should().Be("Chest pain");
        admission.Status.Should().Be(AdmissionStatus.Admitted);
        admission.DischargeDateTime.Should().BeNull();
        admission.CreatedBy.Should().Be(actorId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidReasonForAdmission_ThrowsArgumentException(string invalidReason)
    {
        var act = () => Admission.Create(
            "ADM-2026-000001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow, AdmissionType.Elective, invalidReason, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_UpdatesFieldsAndSetsUpdatedAudit()
    {
        var admission = NewAdmission();
        var newDepartmentId = Guid.NewGuid();
        var newConsultantId = Guid.NewGuid();
        var updatedBy = Guid.NewGuid();

        admission.Update(newDepartmentId, newConsultantId, AdmissionType.Transfer, "Updated reason", updatedBy);

        admission.DepartmentId.Should().Be(newDepartmentId);
        admission.ConsultantId.Should().Be(newConsultantId);
        admission.AdmissionType.Should().Be(AdmissionType.Transfer);
        admission.ReasonForAdmission.Should().Be("Updated reason");
        admission.UpdatedBy.Should().Be(updatedBy);
    }

    [Fact]
    public void TransferBed_UpdatesWardAndBed()
    {
        var admission = NewAdmission();
        var newWardId = Guid.NewGuid();
        var newBedId = Guid.NewGuid();
        var updatedBy = Guid.NewGuid();

        admission.TransferBed(newWardId, newBedId, updatedBy);

        admission.WardId.Should().Be(newWardId);
        admission.BedId.Should().Be(newBedId);
        admission.UpdatedBy.Should().Be(updatedBy);
        // Status is untouched by a transfer — still an active admission.
        admission.Status.Should().Be(AdmissionStatus.Admitted);
    }

    [Fact]
    public void Discharge_SetsStatusAndDischargeFields()
    {
        var admission = NewAdmission();
        var dischargeDateTime = new DateTime(2026, 8, 15, 10, 0, 0, DateTimeKind.Utc);
        var updatedBy = Guid.NewGuid();

        admission.Discharge(dischargeDateTime, DischargeType.Normal, "Resolved", "Rest advised", "Follow up in 1 week", updatedBy);

        admission.Status.Should().Be(AdmissionStatus.Discharged);
        admission.DischargeDateTime.Should().Be(dischargeDateTime);
        admission.DischargeType.Should().Be(DischargeType.Normal);
        admission.FinalDiagnosis.Should().Be("Resolved");
        admission.DischargeNotes.Should().Be("Rest advised");
        admission.FollowUpAdvice.Should().Be("Follow up in 1 week");
        admission.UpdatedBy.Should().Be(updatedBy);
    }
}
