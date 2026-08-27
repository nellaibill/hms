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
