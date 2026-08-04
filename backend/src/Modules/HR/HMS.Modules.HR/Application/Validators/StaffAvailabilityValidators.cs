using FluentValidation;
using HMS.Modules.HR.Contracts;

namespace HMS.Modules.HR.Application.Validators;

/// <summary>
/// Only the four required fields — StaffId, StartDate, EndDate, AvailabilityStatus — per
/// the Phase 4 spec. Reason is optional. No date-order, overlap, leave, or holiday checks;
/// all explicitly out of scope for this phase.
/// </summary>
internal class CreateStaffAvailabilityRequestValidator : AbstractValidator<CreateStaffAvailabilityRequest>
{
    public CreateStaffAvailabilityRequestValidator()
    {
        RuleFor(x => x.StaffId).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();
        RuleFor(x => x.AvailabilityStatus).NotNull();
    }
}

internal class UpdateStaffAvailabilityRequestValidator : AbstractValidator<UpdateStaffAvailabilityRequest>
{
    public UpdateStaffAvailabilityRequestValidator()
    {
        RuleFor(x => x.StaffId).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();
        RuleFor(x => x.AvailabilityStatus).NotNull();
    }
}
