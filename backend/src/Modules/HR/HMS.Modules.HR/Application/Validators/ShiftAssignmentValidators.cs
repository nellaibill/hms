using FluentValidation;
using HMS.Modules.HR.Contracts;

namespace HMS.Modules.HR.Application.Validators;

/// <summary>
/// Only the four required fields — StaffId, DepartmentId, ShiftId, RosterDate — per the
/// Phase 2 spec. No overlap/leave/holiday/weekly-off checks; those are later phases.
/// </summary>
internal class CreateShiftAssignmentRequestValidator : AbstractValidator<CreateShiftAssignmentRequest>
{
    public CreateShiftAssignmentRequestValidator()
    {
        RuleFor(x => x.StaffId).NotEmpty();
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.ShiftId).NotEmpty();
        RuleFor(x => x.RosterDate).NotEmpty();
    }
}

internal class UpdateShiftAssignmentRequestValidator : AbstractValidator<UpdateShiftAssignmentRequest>
{
    public UpdateShiftAssignmentRequestValidator()
    {
        RuleFor(x => x.StaffId).NotEmpty();
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.ShiftId).NotEmpty();
        RuleFor(x => x.RosterDate).NotEmpty();
    }
}
