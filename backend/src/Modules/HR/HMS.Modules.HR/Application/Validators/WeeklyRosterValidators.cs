using FluentValidation;
using HMS.Modules.HR.Contracts;

namespace HMS.Modules.HR.Application.Validators;

/// <summary>
/// Only the two required fields — WeekStartDate, DepartmentId — per the Phase 3 spec. No
/// publish-state validation, no per-department-per-week uniqueness check; both explicitly
/// out of scope for this phase.
/// </summary>
internal class CreateWeeklyRosterRequestValidator : AbstractValidator<CreateWeeklyRosterRequest>
{
    public CreateWeeklyRosterRequestValidator()
    {
        RuleFor(x => x.WeekStartDate).NotEmpty();
        RuleFor(x => x.DepartmentId).NotEmpty();
    }
}

internal class UpdateWeeklyRosterRequestValidator : AbstractValidator<UpdateWeeklyRosterRequest>
{
    public UpdateWeeklyRosterRequestValidator()
    {
        RuleFor(x => x.WeekStartDate).NotEmpty();
        RuleFor(x => x.DepartmentId).NotEmpty();
    }
}

/// <summary>Phase 6 — the only field on the copy request, and the only thing that needs
/// validating: the caller must explicitly choose a destination week.</summary>
internal class CopyWeeklyRosterRequestValidator : AbstractValidator<CopyWeeklyRosterRequest>
{
    public CopyWeeklyRosterRequestValidator()
    {
        RuleFor(x => x.TargetWeekStartDate).NotEmpty();
    }
}

/// <summary>GET /weekly-rosters/monthly's query. Year/Month bounds are DateOnly's own
/// supported range and the calendar's actual month count — a structural fact, not an
/// invented business rule — not a "which months are valid for scheduling" decision.</summary>
internal class MonthlyWeeklyRosterQueryValidator : AbstractValidator<MonthlyWeeklyRosterQuery>
{
    public MonthlyWeeklyRosterQueryValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(1, 9999);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}
