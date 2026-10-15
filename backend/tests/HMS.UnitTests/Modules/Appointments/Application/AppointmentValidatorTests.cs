using FluentAssertions;
using HMS.Modules.Appointments.Application.Validators;
using HMS.Modules.Appointments.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Appointments.Application;

public class AppointmentValidatorTests
{
    private static BookAppointmentRequest NewCaller(string? name = "Meena Raj", string? phone = "9123456780") => new()
    {
        PatientName = name,
        PhoneNumber = phone,
        DepartmentId = Guid.NewGuid(),
        ConsultantId = Guid.NewGuid(),
        AppointmentDate = new DateOnly(2026, 10, 16),
        StartTime = new TimeOnly(9, 0),
    };

    [Fact]
    public void Book_AFirstTimeCaller_NeedsANameAndATenDigitPhone()
    {
        var validator = new BookAppointmentRequestValidator();

        validator.Validate(NewCaller()).IsValid.Should().BeTrue();
        validator.Validate(NewCaller(name: "")).IsValid.Should().BeFalse();
        validator.Validate(NewCaller(phone: "98765")).IsValid.Should().BeFalse();
        validator.Validate(NewCaller(name: "R2D2")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Book_ARegisteredPatient_NeedsNoNameOrPhone()
    {
        var request = NewCaller(name: null, phone: null) with { PatientId = Guid.NewGuid() };

        new BookAppointmentRequestValidator().Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Block_NeedsAnOrderedRangeAndAReason()
    {
        var validator = new CreateConsultantBlockRequestValidator();
        var block = new CreateConsultantBlockRequest
        {
            ConsultantId = Guid.NewGuid(),
            FromDate = new DateOnly(2026, 10, 20),
            ToDate = new DateOnly(2026, 10, 22),
            Reason = "Leave",
        };

        validator.Validate(block).IsValid.Should().BeTrue();
        validator.Validate(block with { ToDate = new DateOnly(2026, 10, 19) }).IsValid.Should().BeFalse();
        validator.Validate(block with { Reason = " " }).IsValid.Should().BeFalse();
        validator.Validate(block with { Session = 3 }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void List_RejectsAReversedOrOverlongRange()
    {
        var validator = new AppointmentListQueryValidator();

        validator.Validate(new AppointmentListQuery { FromDate = new DateOnly(2026, 10, 20), ToDate = new DateOnly(2026, 10, 19) }).IsValid.Should().BeFalse();
        validator.Validate(new AppointmentListQuery { FromDate = new DateOnly(2026, 1, 1), ToDate = new DateOnly(2027, 6, 1) }).IsValid.Should().BeFalse();
        validator.Validate(new AppointmentListQuery { FromDate = new DateOnly(2026, 10, 1), ToDate = new DateOnly(2026, 10, 31) }).IsValid.Should().BeTrue();
    }
}
