using FluentValidation;
using HMS.Modules.Calendar.Contracts;

namespace HMS.Modules.Calendar.Application.Validators;

/// <summary>
/// Structural validation only — Title required, EventType required, StartDate not
/// after EndDate. Business-rule checks that need a database query (Holiday date
/// uniqueness, Department existence) live in EventService instead, per the
/// malformed-request-vs-business-rule split used everywhere else in this codebase.
/// The "Doctor Leave cannot overlap another approved leave" rule is not implemented
/// here or in EventService — see CalendarErrorCodes' doc comment for why.
/// </summary>
internal class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.EventType).NotNull();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();

        RuleFor(x => x)
            .Must(x => x.StartDate <= x.EndDate)
            .WithName("EndDate")
            .WithMessage("EndDate must be on or after StartDate.");
    }
}

internal class UpdateEventRequestValidator : AbstractValidator<UpdateEventRequest>
{
    public UpdateEventRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.EventType).NotNull();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();

        RuleFor(x => x)
            .Must(x => x.StartDate <= x.EndDate)
            .WithName("EndDate")
            .WithMessage("EndDate must be on or after StartDate.");
    }
}

/// <summary>GET /events/month's query. Year/Month bounds are DateOnly's own supported
/// range and the calendar's actual month count — a structural fact, not an invented
/// business rule. Mirrors HR's MonthlyWeeklyRosterQueryValidator.</summary>
internal class MonthlyEventQueryValidator : AbstractValidator<MonthlyEventQuery>
{
    public MonthlyEventQueryValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(1, 9999);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

/// <summary>
/// Validates only the envelope (non-empty, under the batch cap). Each individual item
/// is validated with the same CreateEventRequestValidator POST /events already uses,
/// applied per-item by the controller so a bad row can be reported by index instead of
/// failing the whole batch.
/// </summary>
internal class BulkCreateEventsRequestValidator : AbstractValidator<BulkCreateEventsRequest>
{
    public const int MaxBatchSize = 500;

    public BulkCreateEventsRequestValidator()
    {
        RuleFor(x => x.Events).NotEmpty().WithMessage("At least one event is required.");
        RuleFor(x => x.Events)
            .Must(events => events.Count <= MaxBatchSize)
            .WithMessage($"A single bulk request cannot contain more than {MaxBatchSize} events.")
            .When(x => x.Events is not null);
    }
}
