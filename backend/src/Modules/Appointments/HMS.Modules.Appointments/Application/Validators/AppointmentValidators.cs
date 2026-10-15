using FluentValidation;
using HMS.Modules.Appointments.Contracts;
using HMS.Modules.Appointments.Domain;

namespace HMS.Modules.Appointments.Application.Validators;

internal static class AppointmentValidationRules
{
    // Same rules as patient registration (CreatePatientRequestValidator), so a first-time caller
    // booked here registers cleanly when they arrive.
    public const string PhonePattern = @"^[0-9]{10}$";
    public const string PhonePatternMessage = "Phone number must be exactly 10 digits.";
    public const string NamePattern = @"^[\p{L}\p{M}][\p{L}\p{M}\s'.-]*$";
    public const string NamePatternMessage = "Name must contain letters only.";

    public const int MaxListRangeDays = 366;
}

internal class BookAppointmentRequestValidator : AbstractValidator<BookAppointmentRequest>
{
    public BookAppointmentRequestValidator()
    {
        RuleFor(x => x.ConsultantId).NotEmpty().WithMessage("Select a consultant.");
        RuleFor(x => x.DepartmentId).NotEmpty().WithMessage("Select a department.");
        RuleFor(x => x.AppointmentDate).NotEmpty().WithMessage("Select a date.");
        RuleFor(x => x.Source).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(Appointment.MaxNotesLength);

        When(x => x.PatientId is null, () =>
        {
            RuleFor(x => x.PatientName)
                .NotEmpty().WithMessage("Enter the patient's name, or pick a registered patient.")
                .MaximumLength(Appointment.MaxNameLength)
                .Matches(AppointmentValidationRules.NamePattern).WithMessage(AppointmentValidationRules.NamePatternMessage);
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Enter the patient's phone number.")
                .Matches(AppointmentValidationRules.PhonePattern).WithMessage(AppointmentValidationRules.PhonePatternMessage);
        });

        RuleFor(x => x.PatientId).NotEqual(Guid.Empty).When(x => x.PatientId.HasValue);
    }
}

internal class RescheduleAppointmentRequestValidator : AbstractValidator<RescheduleAppointmentRequest>
{
    public RescheduleAppointmentRequestValidator()
    {
        RuleFor(x => x.AppointmentDate).NotEmpty().WithMessage("Select a date.");
    }
}

internal class CancelAppointmentRequestValidator : AbstractValidator<CancelAppointmentRequest>
{
    public CancelAppointmentRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Enter a reason for cancelling.").MaximumLength(Appointment.MaxReasonLength);
    }
}

internal class MarkAppointmentArrivedRequestValidator : AbstractValidator<MarkAppointmentArrivedRequest>
{
    public MarkAppointmentArrivedRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.VisitId).NotEmpty();
    }
}

internal class AppointmentListQueryValidator : AbstractValidator<AppointmentListQuery>
{
    public AppointmentListQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate!.Value)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("'To' date must be on or after the 'From' date.");
        RuleFor(x => x)
            .Must(x => x.ToDate!.Value.DayNumber - x.FromDate!.Value.DayNumber < AppointmentValidationRules.MaxListRangeDays)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue && x.ToDate >= x.FromDate)
            .WithName("ToDate")
            .WithMessage($"The date range can be at most {AppointmentValidationRules.MaxListRangeDays} days.");
        RuleFor(x => x.Search).MaximumLength(100);
    }
}

internal class AppointmentSlotsQueryValidator : AbstractValidator<AppointmentSlotsQuery>
{
    public AppointmentSlotsQueryValidator()
    {
        RuleFor(x => x.ConsultantId).NotEmpty().WithMessage("Select a consultant.");
        RuleFor(x => x.Date).NotEmpty().WithMessage("Select a date.");
    }
}

internal class AppointmentSummaryQueryValidator : AbstractValidator<AppointmentSummaryQuery>
{
    public AppointmentSummaryQueryValidator()
    {
        RuleFor(x => x.Date).NotEmpty().WithMessage("Select a date.");
    }
}

internal class CreateConsultantBlockRequestValidator : AbstractValidator<CreateConsultantBlockRequest>
{
    public CreateConsultantBlockRequestValidator()
    {
        RuleFor(x => x.ConsultantId).NotEmpty().WithMessage("Select a consultant.");
        RuleFor(x => x.FromDate).NotEmpty().WithMessage("Select the first day of leave.");
        RuleFor(x => x.ToDate).NotEmpty().WithMessage("Select the last day of leave.");
        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("The last day must be on or after the first day.");
        RuleFor(x => x)
            .Must(x => x.ToDate.DayNumber - x.FromDate.DayNumber < ConsultantBlock.MaxDays)
            .When(x => x.ToDate >= x.FromDate)
            .WithName("ToDate")
            .WithMessage($"Leave can cover at most {ConsultantBlock.MaxDays} days at a time.");
        RuleFor(x => x.Session).InclusiveBetween(1, 2).When(x => x.Session.HasValue)
            .WithMessage("Session must be 1 or 2, or empty for the whole day.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Enter a reason.").MaximumLength(Appointment.MaxReasonLength);
    }
}
