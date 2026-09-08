using FluentAssertions;
using HMS.Modules.DischargeSummary.Contracts;
using Xunit;
using DischargeSummaryEntity = HMS.Modules.DischargeSummary.Domain.DischargeSummary;

namespace HMS.UnitTests.Modules.DischargeSummary.Domain;

public class DischargeSummaryTests
{
    [Fact]
    public void Create_WithValidArguments_ReturnsDraftWithPrefilledFinalDiagnosis()
    {
        var admissionId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        var summary = DischargeSummaryEntity.Create(admissionId, patientId, "Acute appendicitis", createdBy: null);

        summary.AdmissionId.Should().Be(admissionId);
        summary.PatientId.Should().Be(patientId);
        summary.Status.Should().Be(DischargeSummaryStatus.Draft);
        summary.FinalDiagnosis.Should().Be("Acute appendicitis");
        summary.Id.Should().NotBeEmpty();
        summary.FinalizedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullFinalDiagnosisPrefill_LeavesFinalDiagnosisNull()
    {
        var summary = DischargeSummaryEntity.Create(Guid.NewGuid(), Guid.NewGuid(), finalDiagnosisPrefill: null, createdBy: null);

        summary.FinalDiagnosis.Should().BeNull();
    }

    [Fact]
    public void Create_WithWhitespaceFinalDiagnosisPrefill_NormalizesToNull()
    {
        var summary = DischargeSummaryEntity.Create(Guid.NewGuid(), Guid.NewGuid(), finalDiagnosisPrefill: "   ", createdBy: null);

        summary.FinalDiagnosis.Should().BeNull();
    }

    [Fact]
    public void Finalize_SetsStatusAndStampsSignOffFields()
    {
        var summary = DischargeSummaryEntity.Create(Guid.NewGuid(), Guid.NewGuid(), "Diagnosis", createdBy: null);
        var preparedBy = Guid.NewGuid();
        var checkedBy = Guid.NewGuid();
        var approvedBy = Guid.NewGuid();
        var finalizedBy = Guid.NewGuid();
        var finalizedAt = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc);

        summary.Finalize(preparedBy, checkedBy, approvedBy, finalizedAt, finalizedBy);

        summary.Status.Should().Be(DischargeSummaryStatus.Finalized);
        summary.PreparedByUserId.Should().Be(preparedBy);
        summary.CheckedByUserId.Should().Be(checkedBy);
        summary.ConsultantApprovedByUserId.Should().Be(approvedBy);
        summary.FinalizedAt.Should().Be(finalizedAt);
        summary.FinalizedByUserId.Should().Be(finalizedBy);
        summary.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Finalize_WithAllSignOffFieldsNull_StillTransitionsToFinalized()
    {
        var summary = DischargeSummaryEntity.Create(Guid.NewGuid(), Guid.NewGuid(), null, createdBy: null);

        summary.Finalize(null, null, null, DateTime.UtcNow, null);

        summary.Status.Should().Be(DischargeSummaryStatus.Finalized);
        summary.PreparedByUserId.Should().BeNull();
    }
}
