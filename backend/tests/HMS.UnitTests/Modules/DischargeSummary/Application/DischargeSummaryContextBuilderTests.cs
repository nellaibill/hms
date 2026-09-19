using FluentAssertions;
using HMS.Modules.DischargeSummary.Application;
using HMS.Modules.IPD.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.DischargeSummary.Application;

public class DischargeSummaryContextBuilderTests
{
    private static AdmissionResponse Admission() => new()
    {
        PatientName = "Jane Roe",
        Uhid = "P-2026-000123",
        Age = 54,
        Gender = "Female",
        WardName = "Ward 3",
        AdmissionType = AdmissionType.Emergency,
        AdmissionDateTime = new DateTime(2026, 9, 1, 10, 0, 0),
        DischargeDateTime = new DateTime(2026, 9, 5, 9, 0, 0),
        DischargeType = DischargeType.Normal,
        ReasonForAdmission = "Acute abdominal pain",
        FinalDiagnosis = "Acute appendicitis",
    };

    private static ProgressNoteResponse Note(int day, string progress) => new()
    {
        NoteDateTime = new DateTime(2026, 9, 1).AddDays(day),
        Progress = progress,
    };

    [Fact]
    public void Build_IncludesClinicalDataButNeverPatientIdentifiers()
    {
        var context = DischargeSummaryContextBuilder.Build(
            Admission(),
            [Note(0, "Post-op day 0, comfortable")],
            [new VitalsReadingResponse { RecordedAt = new DateTime(2026, 9, 1), PulseRate = 88, BloodPressureSystolic = 120, BloodPressureDiastolic = 80 }],
            [new DoctorOrderResponse { OrderedAt = new DateTime(2026, 9, 1), OrderType = DoctorOrderType.Diet, Description = "Soft diet" }],
            [new MedicationOrderResponse { StartDate = new DateTime(2026, 9, 1), DrugName = "Ceftriaxone", Dose = "1 g", Route = "IV", Frequency = "BD", Status = MedicationOrderStatus.Discontinued, DiscontinuedReason = "Course complete" }]);

        context.Should().Contain("54 years, Female");
        context.Should().Contain("Acute appendicitis");
        context.Should().Contain("Post-op day 0, comfortable");
        context.Should().Contain("Pulse 88").And.Contain("BP 120/80");
        context.Should().Contain("Soft diet");
        context.Should().Contain("Ceftriaxone").And.Contain("NOT discharge prescriptions");
        context.Should().NotContain("Jane Roe");
        context.Should().NotContain("P-2026-000123");
    }

    [Fact]
    public void Build_WithVeryLongNotes_KeepsOpeningAndLatestNotesAndMarksTheOmission()
    {
        var notes = Enumerable.Range(0, 60).Select(i => Note(i, $"NOTE-{i:00} " + new string('x', 1_500))).ToList();

        var context = DischargeSummaryContextBuilder.Build(Admission(), notes, [], [], []);

        context.Should().Contain("NOTE-00").And.Contain("NOTE-02");
        context.Should().Contain("NOTE-59");
        context.Should().NotContain("NOTE-10 ");
        context.Should().Contain("omitted for length");
        context.Length.Should().BeLessThan(DischargeSummaryContextBuilder.MaxNotesCharacters + 5_000);
    }
}
