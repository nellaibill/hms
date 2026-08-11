using FluentValidation;
using HMS.Modules.HR.Contracts;

namespace HMS.Modules.HR.Application.Validators;

/// <summary>
/// Only what the spec requires — Code, Name, StartTime, EndTime — plus one cross-field
/// consistency check added later: IsNightShift must agree with whether the shift actually
/// crosses midnight (EndTime earlier than StartTime). Previously IsNightShift was pure,
/// unchecked caller-supplied metadata, which let a shift be saved with a night/day flag
/// that contradicted its own timing (in either direction — a 9am–5pm shift saved as
/// "night", or an actual 22:00–06:00 shift saved as not-night, e.g. by toggling the flag
/// on Edit without changing the times). This only rejects a self-contradictory
/// combination — it still doesn't derive or auto-set the flag.
/// </summary>
internal class CreateShiftRequestValidator : AbstractValidator<CreateShiftRequest>
{
    public CreateShiftRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.StartTime).NotNull();
        RuleFor(x => x.EndTime).NotNull();

        RuleFor(x => x)
            .Must(x => x.IsNightShift == (x.EndTime!.Value < x.StartTime!.Value))
            .WithName("IsNightShift")
            .WithMessage("Night shift should be on only when the end time is earlier than the start time (the shift crosses midnight) — toggle Night shift or adjust the times so they agree.")
            .When(x => x.StartTime.HasValue && x.EndTime.HasValue);
    }
}

internal class UpdateShiftRequestValidator : AbstractValidator<UpdateShiftRequest>
{
    public UpdateShiftRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.StartTime).NotNull();
        RuleFor(x => x.EndTime).NotNull();

        RuleFor(x => x)
            .Must(x => x.IsNightShift == (x.EndTime!.Value < x.StartTime!.Value))
            .WithName("IsNightShift")
            .WithMessage("Night shift should be on only when the end time is earlier than the start time (the shift crosses midnight) — toggle Night shift or adjust the times so they agree.")
            .When(x => x.StartTime.HasValue && x.EndTime.HasValue);
    }
}
