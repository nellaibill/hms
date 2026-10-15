using FluentAssertions;
using HMS.Modules.Appointments.Contracts;
using HMS.Modules.Appointments.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Appointments.Domain;

public class AppointmentTests
{
    private static readonly DateOnly Day = new(2026, 10, 15);
    private static readonly Guid PatientId = Guid.NewGuid();

    private static SlotAssignment Slot(int hour = 9, int minute = 0, int seat = 1, int token = 1)
        => new(Day, new TimeOnly(hour, minute), new TimeOnly(hour, minute).AddMinutes(15), 1, seat, token);

    private static Appointment NewCaller() => Appointment.Book(
        "APT-2026-000001",
        new AppointmentPatient(null, " Meena Raj ", "9876543210", null),
        Guid.NewGuid(),
        Guid.NewGuid(),
        null,
        Slot(),
        AppointmentSource.Phone,
        "  ",
        null);

    [Fact]
    public void Book_StartsBooked_AndKeepsTheSlotAndPatientSnapshot()
    {
        var appointment = NewCaller();

        appointment.Status.Should().Be(AppointmentStatus.Booked);
        appointment.PatientId.Should().BeNull();
        appointment.PatientName.Should().Be("Meena Raj");
        appointment.Notes.Should().BeNull();
        appointment.StartTime.Should().Be(new TimeOnly(9, 0));
        appointment.EndTime.Should().Be(new TimeOnly(9, 15));
        appointment.TokenNumber.Should().Be(1);
        appointment.HoldsSeat.Should().BeTrue();
    }

    [Fact]
    public void Reschedule_MovesTheSlotAndCountsIt()
    {
        var appointment = NewCaller();

        appointment.Reschedule(Slot(10, 30, 1, 7), null);

        appointment.StartTime.Should().Be(new TimeOnly(10, 30));
        appointment.TokenNumber.Should().Be(7);
        appointment.RescheduleCount.Should().Be(1);
    }

    [Fact]
    public void Cancel_RecordsTheReason_AndGivesTheSeatBack()
    {
        var appointment = NewCaller();

        appointment.Cancel("  Patient called to cancel ", null);

        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        appointment.CancelReason.Should().Be("Patient called to cancel");
        appointment.CancelledAt.Should().NotBeNull();
        appointment.HoldsSeat.Should().BeFalse();
    }

    [Fact]
    public void MarkArrived_LinksAFirstTimeCallerToTheRegisteredPatient()
    {
        var appointment = NewCaller();
        var visitId = Guid.NewGuid();

        appointment.MarkArrived(new AppointmentPatient(PatientId, "Meena Rajan", "9876543210", "P-2026-000001"), visitId, null);

        appointment.Status.Should().Be(AppointmentStatus.Arrived);
        appointment.PatientId.Should().Be(PatientId);
        appointment.PatientName.Should().Be("Meena Rajan");
        appointment.Uhid.Should().Be("P-2026-000001");
        appointment.VisitId.Should().Be(visitId);
        appointment.HoldsSeat.Should().BeTrue();
    }

    [Fact]
    public void MarkArrived_ForADifferentRegisteredPatient_Throws()
    {
        var appointment = Appointment.Book(
            "APT-2026-000002",
            new AppointmentPatient(PatientId, "Ravi Kumar", "9876543210", "P-2026-000002"),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            Slot(),
            AppointmentSource.InPerson,
            null,
            null);

        var act = () => appointment.MarkArrived(new AppointmentPatient(Guid.NewGuid(), "Someone Else", "9000000000", "P-2026-000003"), Guid.NewGuid(), null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void OnlyABookedAppointmentCanChange()
    {
        var appointment = NewCaller();
        appointment.MarkNoShow(null);

        appointment.Status.Should().Be(AppointmentStatus.NoShow);
        ((Action)(() => appointment.Cancel("late", null))).Should().Throw<InvalidOperationException>();
        ((Action)(() => appointment.Reschedule(Slot(11), null))).Should().Throw<InvalidOperationException>();
        ((Action)(() => appointment.MarkNoShow(null))).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Book_RejectsASlotThatEndsBeforeItStarts()
    {
        var act = () => Appointment.Book(
            "APT-2026-000003",
            new AppointmentPatient(null, "Meena", "9876543210", null),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            new SlotAssignment(Day, new TimeOnly(9, 15), new TimeOnly(9, 0), 1, 1, 1),
            AppointmentSource.Phone,
            null,
            null);

        act.Should().Throw<ArgumentException>();
    }
}

public class ConsultantBlockTests
{
    [Fact]
    public void Covers_MatchesDatesInRange_AndOnlyItsSessionWhenOneIsSet()
    {
        var wholeDays = ConsultantBlock.Create(Guid.NewGuid(), new DateOnly(2026, 10, 20), new DateOnly(2026, 10, 22), null, "Conference", null);
        var eveningOnly = ConsultantBlock.Create(Guid.NewGuid(), new DateOnly(2026, 10, 20), new DateOnly(2026, 10, 20), 2, "Surgery list", null);

        wholeDays.Covers(new DateOnly(2026, 10, 21), 1).Should().BeTrue();
        wholeDays.Covers(new DateOnly(2026, 10, 23), 1).Should().BeFalse();
        eveningOnly.Covers(new DateOnly(2026, 10, 20), 2).Should().BeTrue();
        eveningOnly.Covers(new DateOnly(2026, 10, 20), 1).Should().BeFalse();
    }

    [Fact]
    public void Create_RejectsAnEndBeforeTheStart()
    {
        var act = () => ConsultantBlock.Create(Guid.NewGuid(), new DateOnly(2026, 10, 22), new DateOnly(2026, 10, 20), null, "Leave", null);

        act.Should().Throw<ArgumentException>();
    }
}
