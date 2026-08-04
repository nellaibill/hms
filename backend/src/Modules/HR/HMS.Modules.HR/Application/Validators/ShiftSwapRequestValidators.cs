using FluentValidation;
using HMS.Modules.HR.Contracts;

namespace HMS.Modules.HR.Application.Validators;

/// <summary>
/// Only the six required fields per the Phase 5 spec — RequestedByStaffId,
/// RequestedToStaffId, CurrentShiftAssignmentId, RequestedShiftAssignmentId, Status,
/// RequestedDate. Remarks is optional. No approval workflow validation, no conflict
/// detection — those are explicitly out of scope. Existence of the referenced
/// ShiftAssignments is checked in ShiftSwapRequestService, not here (this validator only
/// checks presence, matching the malformed-request-vs-business-rule split used everywhere
/// else in this codebase).
/// </summary>
internal class CreateSwapRequestValidator : AbstractValidator<CreateSwapRequest>
{
    public CreateSwapRequestValidator()
    {
        RuleFor(x => x.RequestedByStaffId).NotEmpty();
        RuleFor(x => x.RequestedToStaffId).NotEmpty();
        RuleFor(x => x.CurrentShiftAssignmentId).NotEmpty();
        RuleFor(x => x.RequestedShiftAssignmentId).NotEmpty();
        RuleFor(x => x.Status).NotNull();
        RuleFor(x => x.RequestedDate).NotEmpty();
    }
}

internal class UpdateSwapRequestValidator : AbstractValidator<UpdateSwapRequest>
{
    public UpdateSwapRequestValidator()
    {
        RuleFor(x => x.RequestedByStaffId).NotEmpty();
        RuleFor(x => x.RequestedToStaffId).NotEmpty();
        RuleFor(x => x.CurrentShiftAssignmentId).NotEmpty();
        RuleFor(x => x.RequestedShiftAssignmentId).NotEmpty();
        RuleFor(x => x.Status).NotNull();
        RuleFor(x => x.RequestedDate).NotEmpty();
    }
}
