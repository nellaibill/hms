using FluentValidation;
using HMS.Modules.DischargeSummary.Contracts;

namespace HMS.Modules.DischargeSummary.Application.Validators;

/// <summary>
/// Server-side validation — the authoritative check — mirroring the max lengths enforced by
/// DischargeSummaryConfiguration and simple sanity ranges for the vitals fields. Every field
/// is optional (a doctor fills sections in over time before Finalize), so nothing here uses
/// NotEmpty.
/// </summary>
internal class UpdateDischargeSummaryRequestValidator : AbstractValidator<UpdateDischargeSummaryRequest>
{
    public UpdateDischargeSummaryRequestValidator()
    {
        RuleFor(x => x.FinalDiagnosis).MaximumLength(2000);

        RuleFor(x => x.ChiefComplaints).MaximumLength(2000);
        RuleFor(x => x.HistoryOfPresentingIllness).MaximumLength(8000);
        RuleFor(x => x.PastMedicalHistory).MaximumLength(2000);
        RuleFor(x => x.PastSurgicalHistory).MaximumLength(2000);
        RuleFor(x => x.FamilyHistory).MaximumLength(2000);
        RuleFor(x => x.PersonalHistory).MaximumLength(2000);

        RuleFor(x => x.GeneralExamination).MaximumLength(2000);
        RuleFor(x => x.CvsFindings).MaximumLength(1000);
        RuleFor(x => x.RsFindings).MaximumLength(1000);
        RuleFor(x => x.PaFindings).MaximumLength(1000);
        RuleFor(x => x.CnsFindings).MaximumLength(1000);
        RuleFor(x => x.LocalExamination).MaximumLength(2000);
        RuleFor(x => x.Gait).MaximumLength(500);

        RuleFor(x => x.HeightCm).InclusiveBetween(0, 300).When(x => x.HeightCm.HasValue);
        RuleFor(x => x.WeightKg).InclusiveBetween(0, 500).When(x => x.WeightKg.HasValue);
        RuleFor(x => x.PulseRate).InclusiveBetween(0, 300).When(x => x.PulseRate.HasValue);
        RuleFor(x => x.RespiratoryRate).InclusiveBetween(0, 150).When(x => x.RespiratoryRate.HasValue);
        RuleFor(x => x.TemperatureF).InclusiveBetween(70, 115).When(x => x.TemperatureF.HasValue);
        RuleFor(x => x.SpO2Percent).InclusiveBetween(0, 100).When(x => x.SpO2Percent.HasValue);
        RuleFor(x => x.BloodPressure).MaximumLength(20);

        RuleFor(x => x.CourseInHospital).MaximumLength(8000);

        RuleFor(x => x.ProcedureName).MaximumLength(500);
        RuleFor(x => x.PrimarySurgeon).MaximumLength(500);
        RuleFor(x => x.AssistantSurgeons).MaximumLength(1000);
        RuleFor(x => x.Anaesthetist).MaximumLength(500);
        RuleFor(x => x.Anaesthesia).MaximumLength(500);
        RuleFor(x => x.SurgicalPosition).MaximumLength(500);
        RuleFor(x => x.IntraOperativeFindings).MaximumLength(8000);
        RuleFor(x => x.OperativeNotes).MaximumLength(8000);

        RuleFor(x => x.Diet).MaximumLength(2000);
        RuleFor(x => x.WoundCare).MaximumLength(2000);
        RuleFor(x => x.Activity).MaximumLength(2000);
        RuleFor(x => x.Physiotherapy).MaximumLength(2000);
        RuleFor(x => x.ReviewInstructions).MaximumLength(2000);
        RuleFor(x => x.EmergencyInstructions).MaximumLength(2000);
        RuleFor(x => x.ConditionAtDischarge).MaximumLength(2000);
    }
}
