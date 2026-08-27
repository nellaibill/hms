using FluentValidation;
using HMS.Modules.HR.Contracts;

namespace HMS.Modules.HR.Application.Validators;

internal class CreateAttendanceRequestValidator : AbstractValidator<CreateAttendanceRequest>
{
    public CreateAttendanceRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.AttendanceDate).NotNull();
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

internal class UpdateAttendanceRequestValidator : AbstractValidator<UpdateAttendanceRequest>
{
    public UpdateAttendanceRequestValidator()
    {
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

internal class CheckInRequestValidator : AbstractValidator<CheckInRequest>
{
    public CheckInRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
    }
}

internal class CheckOutRequestValidator : AbstractValidator<CheckOutRequest>
{
    public CheckOutRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
    }
}
