using FluentAssertions;
using HMS.Modules.Appointments.Application;
using Xunit;

namespace HMS.UnitTests.Modules.Appointments.Application;

public class SlotPlannerTests
{
    // 15 Oct 2026 is a Thursday.
    private static readonly DateOnly Thursday = new(2026, 10, 15);

    private static ConsultantSchedule Schedule(int slotMinutes = 15, int patientsPerSlot = 1, params ScheduleSession[] sessions) => new(
        new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Thursday },
        sessions.Length > 0 ? sessions : [new ScheduleSession(1, new TimeOnly(9, 0), new TimeOnly(10, 0))],
        slotMinutes,
        patientsPerSlot);

    [Fact]
    public void PlanDay_CutsTheSessionIntoEqualSlots()
    {
        var slots = SlotPlanner.PlanDay(Schedule(), Thursday);

        slots.Select(s => (s.Start, s.End)).Should().Equal(
            (new TimeOnly(9, 0), new TimeOnly(9, 15)),
            (new TimeOnly(9, 15), new TimeOnly(9, 30)),
            (new TimeOnly(9, 30), new TimeOnly(9, 45)),
            (new TimeOnly(9, 45), new TimeOnly(10, 0)));
    }

    [Fact]
    public void PlanDay_OnANonClinicDay_ReturnsNoSlots()
    {
        SlotPlanner.PlanDay(Schedule(), Thursday.AddDays(1)).Should().BeEmpty();
    }

    [Fact]
    public void PlanDay_DropsAPartialLastSlot()
    {
        var slots = SlotPlanner.PlanDay(Schedule(sessions: new ScheduleSession(1, new TimeOnly(9, 0), new TimeOnly(9, 40))), Thursday);

        slots.Should().HaveCount(2);
        slots[^1].End.Should().Be(new TimeOnly(9, 30));
    }

    [Fact]
    public void PlanDay_NumbersSlotsThroughBothSessionsInTimeOrder()
    {
        // Session 2 listed first on purpose — the planner sorts by start time.
        var schedule = Schedule(30, 1,
            new ScheduleSession(2, new TimeOnly(17, 0), new TimeOnly(18, 0)),
            new ScheduleSession(1, new TimeOnly(9, 0), new TimeOnly(10, 0)));

        var slots = SlotPlanner.PlanDay(schedule, Thursday);

        slots.Select(s => (s.Session, s.Start, s.Index)).Should().Equal(
            (1, new TimeOnly(9, 0), 0),
            (1, new TimeOnly(9, 30), 1),
            (2, new TimeOnly(17, 0), 2),
            (2, new TimeOnly(17, 30), 3));
    }

    [Fact]
    public void Tokens_RunThroughTheDayWithSeveralPatientsPerSlot()
    {
        var slots = SlotPlanner.PlanDay(Schedule(patientsPerSlot: 2), Thursday);

        slots[0].FirstToken(2).Should().Be(1);
        slots[0].TokenFor(2, 2).Should().Be(2);
        slots[1].FirstToken(2).Should().Be(3);
        slots[3].TokenFor(2, 2).Should().Be(8);
    }

    [Fact]
    public void FirstFreeSeat_ReturnsTheLowestOpenSeat_OrNullWhenFull()
    {
        SlotPlanner.FirstFreeSeat([], 1).Should().Be(1);
        SlotPlanner.FirstFreeSeat([1, 3], 3).Should().Be(2);
        SlotPlanner.FirstFreeSeat([1, 2], 2).Should().BeNull();
    }
}
