using FluentValidation;
using HMS.Modules.IPD.Contracts;

namespace HMS.Modules.IPD.Application.Validators;

internal class CreateAdmissionRequestValidator : AbstractValidator<CreateAdmissionRequest>
{
    public CreateAdmissionRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.ConsultantId).NotEmpty();
        RuleFor(x => x.WardId).NotEmpty();
        RuleFor(x => x.BedId).NotEmpty();
        RuleFor(x => x.AdmissionType).IsInEnum();
        RuleFor(x => x.ReasonForAdmission).NotEmpty().MaximumLength(500);
    }
}

internal class UpdateAdmissionRequestValidator : AbstractValidator<UpdateAdmissionRequest>
{
    public UpdateAdmissionRequestValidator()
    {
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.ConsultantId).NotEmpty();
        RuleFor(x => x.AdmissionType).IsInEnum();
        RuleFor(x => x.ReasonForAdmission).NotEmpty().MaximumLength(500);
    }
}

internal class TransferBedRequestValidator : AbstractValidator<TransferBedRequest>
{
    public TransferBedRequestValidator()
    {
        RuleFor(x => x.NewWardId).NotEmpty();
        RuleFor(x => x.NewBedId).NotEmpty();
        RuleFor(x => x.TransferReason).MaximumLength(500);
    }
}

internal class DischargeAdmissionRequestValidator : AbstractValidator<DischargeAdmissionRequest>
{
    public DischargeAdmissionRequestValidator()
    {
        RuleFor(x => x.DischargeDateTime).NotEmpty();
        RuleFor(x => x.DischargeType).IsInEnum();
        RuleFor(x => x.FinalDiagnosis).MaximumLength(1000);
        RuleFor(x => x.DischargeNotes).MaximumLength(2000);
        RuleFor(x => x.FollowUpAdvice).MaximumLength(2000);
    }
}
