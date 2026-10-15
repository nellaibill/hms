using FluentAssertions;
using HMS.Modules.Masters.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Masters.Domain;

public class ConsultantSlotSettingsTests
{
    private static Consultant Create(int? slotMinutes, int? patientsPerSlot) => Consultant.Create(
        "Dr. Anand",
        departmentId: null,
        specialization: null,
        isActive: true,
        priority: null,
        availableDays: ["Monday"],
        visitStartTime: new TimeOnly(9, 0),
        visitEndTime: new TimeOnly(13, 0),
        consultationTypes: [],
        createdBy: null,
        appointmentSlotMinutes: slotMinutes,
        patientsPerSlot: patientsPerSlot);

    [Fact]
    public void Create_KeepsTheSlotSettings_AndLeavesThemBlankByDefault()
    {
        var consultant = Create(20, 2);

        consultant.AppointmentSlotMinutes.Should().Be(20);
        consultant.PatientsPerSlot.Should().Be(2);
        Create(null, null).AppointmentSlotMinutes.Should().BeNull();
    }

    [Theory]
    [InlineData(4, null)]
    [InlineData(241, null)]
    [InlineData(null, 0)]
    [InlineData(null, 51)]
    public void Create_RejectsOutOfRangeSlotSettings(int? slotMinutes, int? patientsPerSlot)
    {
        var act = () => Create(slotMinutes, patientsPerSlot);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ReplacesTheSlotSettings()
    {
        var consultant = Create(20, 2);

        consultant.Update("Dr. Anand", null, null, true, null, ["Monday"], new TimeOnly(9, 0), new TimeOnly(13, 0), [], null, appointmentSlotMinutes: 10, patientsPerSlot: null);

        consultant.AppointmentSlotMinutes.Should().Be(10);
        consultant.PatientsPerSlot.Should().BeNull();
    }
}
