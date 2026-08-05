using FluentValidation;
using HMS.Modules.HR.Contracts;

namespace HMS.Modules.HR.Application.Validators;

/// <summary>
/// The four required fields — StaffId, StartDate, EndDate, AvailabilityStatus — per the
/// Phase 4 spec, plus one cross-field consistency check added later: EndDate must not be
/// before StartDate (a date range that ends before it starts is never meaningful, unlike
/// overlap/leave/holiday checks, which are genuinely optional business rules and remain
/// out of scope). Reason is optional.
/// </summary>
internal class CreateStaffAvailabilityRequestValidator : AbstractValidator<CreateStaffAvailabilityRequest>
{
    public CreateStaffAvailabilityRequestValidator()
    {
        RuleFor(x => x.StaffId).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();
        RuleFor(x => x.AvailabilityStatus).NotNull();

        RuleFor(x => x)
            .Must(x => x.EndDate >= x.StartDate)
            .WithName("EndDate")
            .WithMessage("EndDate must be on or after StartDate.");
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

        RuleFor(x => x)
            .Must(x => x.EndDate >= x.StartDate)
            .WithName("EndDate")
            .WithMessage("EndDate must be on or after StartDate.");
    }
}
