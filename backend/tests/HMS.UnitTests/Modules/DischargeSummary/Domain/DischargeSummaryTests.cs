using FluentAssertions;
using HMS.Modules.DischargeSummary.Contracts;
using Xunit;
using DischargeMedicationEntity = HMS.Modules.DischargeSummary.Domain.DischargeMedication;
using DischargeSummaryEntity = HMS.Modules.DischargeSummary.Domain.DischargeSummary;

namespace HMS.UnitTests.Modules.DischargeSummary.Domain;

public class DischargeSummaryTests
{
    [Fact]
    public void ReplaceMedications_WithNewLines_SetsMedicationsCollection()
    {
        var summary = DischargeSummaryEntity.Create(Guid.NewGuid(), Guid.NewGuid(), "Diagnosis", createdBy: null);
        var line1 = DischargeMedicationEntity.Create(summary.Id, 1, "Paracetamol", "500mg", "Oral", 1, 0, 1, 1, 5, FoodInstruction.AfterFood, null);
        var line2 = DischargeMedicationEntity.Create(summary.Id, 2, "Amoxicillin", "250mg", "Oral", 1, 1, 1, 0, 7, FoodInstruction.BeforeFood, null);

        summary.ReplaceMedications([line1, line2], updatedBy: null);

        summary.Medications.Should().HaveCount(2);
        summary.Medications.Should().Contain(m => m.DrugName == "Paracetamol");
        summary.Medications.Should().Contain(m => m.DrugName == "Amoxicillin");
        summary.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void ReplaceMedications_CalledTwice_DiscardsThePreviousList()
    {
        var summary = DischargeSummaryEntity.Create(Guid.NewGuid(), Guid.NewGuid(), "Diagnosis", createdBy: null);
        var first = DischargeMedicationEntity.Create(summary.Id, 1, "Paracetamol", "500mg", "Oral", 1, 0, 1, 1, 5, FoodInstruction.AfterFood, null);
        summary.ReplaceMedications([first], updatedBy: null);

        var second = DischargeMedicationEntity.Create(summary.Id, 1, "Ibuprofen", "400mg", "Oral", 1, 0, 0, 1, 3, FoodInstruction.AfterFood, null);
        summary.ReplaceMedications([second], updatedBy: null);

        summary.Medications.Should().ContainSingle();
        summary.Medications.Single().DrugName.Should().Be("Ibuprofen");
    }

    [Fact]
    public void ReplaceMedications_WithEmptyList_ClearsMedications()
    {
        var summary = DischargeSummaryEntity.Create(Guid.NewGuid(), Guid.NewGuid(), "Diagnosis", createdBy: null);
        var line = DischargeMedicationEntity.Create(summary.Id, 1, "Paracetamol", "500mg", "Oral", 1, 0, 1, 1, 5, FoodInstruction.AfterFood, null);
        summary.ReplaceMedications([line], updatedBy: null);

        summary.ReplaceMedications([], updatedBy: null);

        summary.Medications.Should().BeEmpty();
    }

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
    public void UpdateClinicalDetails_SetsEveryFieldAndTrimsFreeText()
    {
        var summary = DischargeSummaryEntity.Create(Guid.NewGuid(), Guid.NewGuid(), "Initial diagnosis", createdBy: null);
        var updatedBy = Guid.NewGuid();
        var procedureDateTime = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

        summary.UpdateClinicalDetails(
            finalDiagnosis: "  Revised diagnosis  ",
            chiefComplaints: "Pain",
            historyOfPresentingIllness: "HPI text",
            pastMedicalHistory: "PMH text",
            pastSurgicalHistory: "PSH text",
            familyHistory: "FH text",
            personalHistory: "PH text",
            generalExamination: "GE text",
            cvsFindings: "S1S2 normal",
            rsFindings: "Clear",
            paFindings: "Soft",
            cnsFindings: "Intact",
            localExamination: "Local text",
            gait: "Normal",
            heightCm: 170.5m,
            weightKg: 68.2m,
            pulseRate: 78,
            respiratoryRate: 16,
            temperatureF: 98.6m,
            spO2Percent: 98,
            bloodPressure: "120/80",
            courseInHospital: "Uneventful",
            procedureName: "  Appendectomy  ",
            procedureDateTime: procedureDateTime,
            primarySurgeon: "Dr. Smith",
            assistantSurgeons: "Dr. Jones",
            anaesthetist: "Dr. Lee",
            anaesthesia: "General",
            surgicalPosition: "Supine",
            intraOperativeFindings: "Inflamed appendix",
            operativeNotes: "Uncomplicated",
            diet: "Soft diet",
            woundCare: "Keep dry",
            activity: "Light activity",
            physiotherapy: "Not required",
            reviewInstructions: "Review in 1 week",
            emergencyInstructions: "Return if fever",
            conditionAtDischarge: "Stable",
            updatedBy: updatedBy);

        summary.FinalDiagnosis.Should().Be("Revised diagnosis");
        summary.ChiefComplaints.Should().Be("Pain");
        summary.HeightCm.Should().Be(170.5m);
        summary.PulseRate.Should().Be(78);
        summary.BloodPressure.Should().Be("120/80");
        summary.CourseInHospital.Should().Be("Uneventful");
        summary.ProcedureName.Should().Be("Appendectomy");
        summary.ProcedureDateTime.Should().Be(procedureDateTime);
        summary.ConditionAtDischarge.Should().Be("Stable");
        summary.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateClinicalDetails_WithWhitespaceFields_NormalizesToNull()
    {
        var summary = DischargeSummaryEntity.Create(Guid.NewGuid(), Guid.NewGuid(), "Diagnosis", createdBy: null);

        summary.UpdateClinicalDetails(
            finalDiagnosis: "   ",
            chiefComplaints: null,
            historyOfPresentingIllness: null,
            pastMedicalHistory: null,
            pastSurgicalHistory: null,
            familyHistory: null,
            personalHistory: null,
            generalExamination: null,
            cvsFindings: null,
            rsFindings: null,
            paFindings: null,
            cnsFindings: null,
            localExamination: null,
            gait: null,
            heightCm: null,
            weightKg: null,
            pulseRate: null,
            respiratoryRate: null,
            temperatureF: null,
            spO2Percent: null,
            bloodPressure: "   ",
            courseInHospital: null,
            procedureName: null,
            procedureDateTime: null,
            primarySurgeon: null,
            assistantSurgeons: null,
            anaesthetist: null,
            anaesthesia: null,
            surgicalPosition: null,
            intraOperativeFindings: null,
            operativeNotes: null,
            diet: null,
            woundCare: null,
            activity: null,
            physiotherapy: null,
            reviewInstructions: null,
            emergencyInstructions: null,
            conditionAtDischarge: null,
            updatedBy: null);

        summary.FinalDiagnosis.Should().BeNull();
        summary.BloodPressure.Should().BeNull();
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
