using FluentAssertions;
using HMS.Modules.Masters.Application.Validators;
using HMS.Modules.Masters.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Masters.Application;

// ADR-148: a consultant's schedule is all or nothing — days with both visiting times, or none at
// all for an on-call or visiting doctor, who can then still be saved and given a login.
public class ConsultantScheduleValidationTests
{
    private static readonly IReadOnlyList<ConsultationTypeChargeDto> OneType = [new(Guid.NewGuid(), null)];

    private static UpdateConsultantRequest Request(string[] days, TimeOnly? start, TimeOnly? end) => new()
    {
        Name = "Dr. M. Ramalakshmi, D.G.O.",
        AvailableDays = days,
        VisitStartTime = start,
        VisitEndTime = end,
        ConsultationTypeCharges = OneType,
    };

    [Fact]
    public void NoScheduleAtAll_IsValid()
    {
        new UpdateConsultantRequestValidator().Validate(Request([], null, null)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void DaysWithBothTimes_IsValid()
    {
        new UpdateConsultantRequestValidator().Validate(Request(["Monday", "Saturday"], new TimeOnly(10, 0), new TimeOnly(16, 0))).IsValid.Should().BeTrue();
    }

    [Fact]
    public void DaysWithoutTimes_IsInvalid()
    {
        new UpdateConsultantRequestValidator().Validate(Request(["Monday"], null, null)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void TimesWithoutDays_IsInvalid()
    {
        new CreateConsultantRequestValidator().Validate(new CreateConsultantRequest
        {
            Name = "Dr. P. Anitha",
            AvailableDays = [],
            VisitStartTime = new TimeOnly(9, 0),
            VisitEndTime = new TimeOnly(17, 0),
            ConsultationTypeCharges = OneType,
        }).IsValid.Should().BeFalse();
    }
}
