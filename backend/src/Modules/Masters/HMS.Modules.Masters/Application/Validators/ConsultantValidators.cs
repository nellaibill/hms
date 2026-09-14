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

        RuleFor(x => x.AvailableDays).NotEmpty().WithMessage("Select at least one available day.");
        RuleForEach(x => x.AvailableDays).Must(BeAValidDayName).WithMessage("'{PropertyValue}' is not a valid day of the week.");
        RuleFor(x => x.VisitStartTime).NotNull().WithMessage("Visit start time is required.");
        RuleFor(x => x.VisitEndTime).NotNull().WithMessage("Visit end time is required.");
        RuleFor(x => x.VisitEndTime)
            .GreaterThan(x => x.VisitStartTime)
            .When(x => x.VisitStartTime.HasValue && x.VisitEndTime.HasValue)
            .WithMessage("Visit end time must be after the start time.");

        RuleFor(x => x.ConsultationTypeIds).NotEmpty().WithMessage("Select at least one consultation type.");
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

        RuleFor(x => x.AvailableDays).NotEmpty().WithMessage("Select at least one available day.");
        RuleForEach(x => x.AvailableDays).Must(CreateConsultantRequestValidator.BeAValidDayName).WithMessage("'{PropertyValue}' is not a valid day of the week.");
        RuleFor(x => x.VisitStartTime).NotNull().WithMessage("Visit start time is required.");
        RuleFor(x => x.VisitEndTime).NotNull().WithMessage("Visit end time is required.");
        RuleFor(x => x.VisitEndTime)
            .GreaterThan(x => x.VisitStartTime)
            .When(x => x.VisitStartTime.HasValue && x.VisitEndTime.HasValue)
            .WithMessage("Visit end time must be after the start time.");

        RuleFor(x => x.ConsultationTypeIds).NotEmpty().WithMessage("Select at least one consultation type.");
    }
}
