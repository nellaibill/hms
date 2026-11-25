using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateConsultantRequestValidator : AbstractValidator<CreateConsultantRequest>
{
    public CreateConsultantRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Specialization).MaximumLength(150);
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(1).When(x => x.Priority.HasValue)
            .WithMessage("Priority must be 1 or greater.");

        // A schedule is all or nothing (ADR-148): days and both visiting times together, or none
        // at all for an on-call or visiting doctor with no fixed days. Requiring one used to stop
        // a login being created for the 26 consultants that have none (25 Nov staging test).
        RuleFor(x => x.AvailableDays).NotEmpty().When(x => x.VisitStartTime.HasValue || x.VisitEndTime.HasValue)
            .WithMessage("Select at least one available day, or clear the visiting hours for a doctor with no fixed schedule.");
        RuleForEach(x => x.AvailableDays).Must(BeAValidDayName).WithMessage("'{PropertyValue}' is not a valid day of the week.");
        RuleFor(x => x.VisitStartTime).NotNull().When(x => x.AvailableDays.Count > 0 || x.VisitEndTime.HasValue).WithMessage("Visit start time is required.");
        RuleFor(x => x.VisitEndTime).NotNull().When(x => x.AvailableDays.Count > 0 || x.VisitStartTime.HasValue).WithMessage("Visit end time is required.");
        RuleFor(x => x.VisitEndTime)
            .GreaterThan(x => x.VisitStartTime)
            .When(x => x.VisitStartTime.HasValue && x.VisitEndTime.HasValue)
            .WithMessage("Visit end time must be after the start time.");

        // Optional second session — both or neither, end after start.
        RuleFor(x => x.VisitEndTime2).NotNull().When(x => x.VisitStartTime2.HasValue).WithMessage("Second visit end time is required.");
        RuleFor(x => x.VisitStartTime2).NotNull().When(x => x.VisitEndTime2.HasValue).WithMessage("Second visit start time is required.");
        RuleFor(x => x.VisitEndTime2)
            .GreaterThan(x => x.VisitStartTime2)
            .When(x => x.VisitStartTime2.HasValue && x.VisitEndTime2.HasValue)
            .WithMessage("Second visit end time must be after its start time.");

        RuleFor(x => x.ConsultationTypeCharges).NotEmpty().WithMessage("Select at least one consultation type.");
        RuleForEach(x => x.ConsultationTypeCharges)
            .ChildRules(charge => charge.RuleFor(c => c.ConsultantCharge).GreaterThanOrEqualTo(0).When(c => c.ConsultantCharge.HasValue)
                .WithMessage("Consultant charge cannot be negative."));
    }

    internal static bool BeAValidDayName(string day) => Enum.TryParse<DayOfWeek>(day, out _);
}

internal class UpdateConsultantRequestValidator : AbstractValidator<UpdateConsultantRequest>
{
    public UpdateConsultantRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Specialization).MaximumLength(150);
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(1).When(x => x.Priority.HasValue)
            .WithMessage("Priority must be 1 or greater.");

        // A schedule is all or nothing (ADR-148): days and both visiting times together, or none
        // at all for an on-call or visiting doctor with no fixed days. Requiring one used to stop
        // a login being created for the 26 consultants that have none (25 Nov staging test).
        RuleFor(x => x.AvailableDays).NotEmpty().When(x => x.VisitStartTime.HasValue || x.VisitEndTime.HasValue)
            .WithMessage("Select at least one available day, or clear the visiting hours for a doctor with no fixed schedule.");
        RuleForEach(x => x.AvailableDays).Must(CreateConsultantRequestValidator.BeAValidDayName).WithMessage("'{PropertyValue}' is not a valid day of the week.");
        RuleFor(x => x.VisitStartTime).NotNull().When(x => x.AvailableDays.Count > 0 || x.VisitEndTime.HasValue).WithMessage("Visit start time is required.");
        RuleFor(x => x.VisitEndTime).NotNull().When(x => x.AvailableDays.Count > 0 || x.VisitStartTime.HasValue).WithMessage("Visit end time is required.");
        RuleFor(x => x.VisitEndTime)
            .GreaterThan(x => x.VisitStartTime)
            .When(x => x.VisitStartTime.HasValue && x.VisitEndTime.HasValue)
            .WithMessage("Visit end time must be after the start time.");

        RuleFor(x => x.VisitEndTime2).NotNull().When(x => x.VisitStartTime2.HasValue).WithMessage("Second visit end time is required.");
        RuleFor(x => x.VisitStartTime2).NotNull().When(x => x.VisitEndTime2.HasValue).WithMessage("Second visit start time is required.");
        RuleFor(x => x.VisitEndTime2)
            .GreaterThan(x => x.VisitStartTime2)
            .When(x => x.VisitStartTime2.HasValue && x.VisitEndTime2.HasValue)
            .WithMessage("Second visit end time must be after its start time.");

        RuleFor(x => x.ConsultationTypeCharges).NotEmpty().WithMessage("Select at least one consultation type.");
        RuleForEach(x => x.ConsultationTypeCharges)
            .ChildRules(charge => charge.RuleFor(c => c.ConsultantCharge).GreaterThanOrEqualTo(0).When(c => c.ConsultantCharge.HasValue)
                .WithMessage("Consultant charge cannot be negative."));
    }
}
