using FluentValidation;
using HMS.Modules.HR.Contracts;

namespace HMS.Modules.HR.Application.Validators;

internal class CreateLeaveRequestRequestValidator : AbstractValidator<CreateLeaveRequestRequest>
{
    public CreateLeaveRequestRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.StartDate).NotNull();
        RuleFor(x => x.EndDate).NotNull();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);

        RuleFor(x => x)
            .Must(x => x.EndDate!.Value >= x.StartDate!.Value)
            .WithName("EndDate")
            .WithMessage("EndDate must not be earlier than StartDate.")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
    }
}

internal class ApproveLeaveRequestRequestValidator : AbstractValidator<ApproveLeaveRequestRequest>
{
    public ApproveLeaveRequestRequestValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

/// <summary>A rejection reason is required, per the HR MVP spec — the one meaningful
/// difference from ApproveLeaveRequestRequest, where a note is optional.</summary>
internal class RejectLeaveRequestRequestValidator : AbstractValidator<RejectLeaveRequestRequest>
{
    public RejectLeaveRequestRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
