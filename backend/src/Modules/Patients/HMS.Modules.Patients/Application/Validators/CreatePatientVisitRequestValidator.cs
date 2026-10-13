using FluentValidation;
using HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Patients.Application.Validators;

/// <summary>
/// Server-side validation — the authoritative check. Existence of the referenced
/// Department/Consultant/AppointmentType/ConsultationType ids is checked in
/// PatientVisitService (a cross-module lookup, not something FluentValidation alone can do),
/// not here.
/// </summary>
internal class CreatePatientVisitRequestValidator : AbstractValidator<CreatePatientVisitRequest>
{
    public CreatePatientVisitRequestValidator()
    {
        RuleFor(x => x.VisitType).IsInEnum();
        RuleFor(x => x.Consultations)
            .NotEmpty().WithMessage("At least one consultation (department + consultant) is required.")
            .Must(c => c.Count <= 4).WithMessage("Up to four consultations (one primary + three additional) are allowed per visit.");
        RuleForEach(x => x.Consultations).SetValidator(new VisitConsultationRequestValidator());

        // Admission details (non-OP visits). AdmissionType itself is deliberately not required
        // here even though the Add Visit form requires it: the New Patient Registration wizard
        // records non-OP visits through this same request without collecting these fields yet,
        // and would be locked out. What IS enforced is that an answer is never half-given.
        RuleFor(x => x.AdmissionType).IsInEnum();
        RuleFor(x => x.ReferredBy).IsInEnum();
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.ArNumber).MaximumLength(50);
        RuleFor(x => x.ArNumber)
            .NotEmpty().WithMessage("A.R number is required for an MLC.")
            .When(x => x.VisitType != VisitType.OP && x.AdmissionType == MedicoLegalType.MLC);
        RuleFor(x => x.ReferredBy)
            .NotNull().WithMessage("Select who referred the patient.")
            .When(x => x.VisitType != VisitType.OP && x.IsReferred == true);
    }
}

internal class VisitConsultationRequestValidator : AbstractValidator<VisitConsultationRequest>
{
    public VisitConsultationRequestValidator()
    {
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.ConsultantId).NotEmpty();
    }
}
