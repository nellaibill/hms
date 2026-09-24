using FluentValidation;
using HMS.Modules.OpdConsultation.Contracts;

namespace HMS.Modules.OpdConsultation.Application.Validators;

/// <summary>
/// Format checks only — no required fields here, since this same request shape backs both
/// SaveDraft (nothing required) and Complete (PresentingComplaints/HeightCm/WeightKg required,
/// enforced by OpdConsultationService.CompleteAsync directly — see that method's own comment on
/// why a second FluentValidation validator for this same request type would collide in DI).
/// </summary>
internal class SaveOpdConsultationRequestValidator : AbstractValidator<SaveOpdConsultationRequest>
{
    public SaveOpdConsultationRequestValidator()
    {
        RuleFor(x => x.HeightCm).GreaterThan(0).When(x => x.HeightCm.HasValue).WithMessage("Height must be greater than 0.");
        RuleFor(x => x.WeightKg).GreaterThan(0).When(x => x.WeightKg.HasValue).WithMessage("Weight must be greater than 0.");
        RuleFor(x => x.PulseRate).GreaterThan(0).When(x => x.PulseRate.HasValue).WithMessage("Pulse rate must be greater than 0.");
        RuleFor(x => x.SpO2Percent).InclusiveBetween(0, 100).When(x => x.SpO2Percent.HasValue).WithMessage("SpO2 must be between 0 and 100.");
        RuleFor(x => x.BloodPressure).MaximumLength(20);

        RuleFor(x => x.PresentingComplaints).MaximumLength(2000);
        RuleFor(x => x.ClinicalHistory).MaximumLength(4000);
        RuleFor(x => x.ExaminationFindings).MaximumLength(4000);
        RuleFor(x => x.PlanOfManagement).MaximumLength(4000);
        RuleFor(x => x.FollowUpInstructions).MaximumLength(2000);
        RuleFor(x => x.EmergencyReviewInstructions).MaximumLength(2000);
        RuleFor(x => x.ReferralReason).MaximumLength(1000);

        RuleForEach(x => x.Investigations).ChildRules(investigation =>
        {
            investigation.RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
            investigation.RuleFor(i => i.Department).IsInEnum();
            investigation.RuleFor(i => i.Priority).IsInEnum();
        });

        // OPD-02: a prescribed medicine must at least name the drug; the rest is optional so a
        // doctor can write "Paracetamol 500 mg SOS" as briefly as they would on paper.
        RuleForEach(x => x.Prescriptions).ChildRules(prescription =>
        {
            prescription.RuleFor(p => p.DrugName).NotEmpty().WithMessage("Drug name is required.").MaximumLength(200);
            prescription.RuleFor(p => p.Dose).MaximumLength(100);
            prescription.RuleFor(p => p.Route).MaximumLength(50);
            prescription.RuleFor(p => p.Frequency).MaximumLength(100);
            prescription.RuleFor(p => p.DurationDays).InclusiveBetween(1, 365).When(p => p.DurationDays.HasValue).WithMessage("Duration must be between 1 and 365 days.");
            prescription.RuleFor(p => p.Instructions).MaximumLength(500);
        });
        RuleFor(x => x.Prescriptions).Must(p => p.Count <= 30).WithMessage("A consultation can have at most 30 prescribed medicines.");
    }
}

/// <summary>OPD-03: adding a diagnosis from the consultation form — same limits as Masters'
/// own Diagnosis validator (name required, 200 chars; ICD code 20 chars).</summary>
internal class CreateOpdDiagnosisRequestValidator : AbstractValidator<CreateOpdDiagnosisRequest>
{
    public CreateOpdDiagnosisRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Diagnosis name is required.").MaximumLength(200);
        RuleFor(x => x.IcdCode).MaximumLength(20);
    }
}

internal class StructureConsultationNoteRequestValidator : AbstractValidator<StructureConsultationNoteRequest>
{
    public StructureConsultationNoteRequestValidator()
    {
        RuleFor(x => x.Transcript).NotEmpty().MaximumLength(20_000);
    }
}
