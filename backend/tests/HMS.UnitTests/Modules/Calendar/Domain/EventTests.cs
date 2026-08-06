using FluentAssertions;
using HMS.Modules.Calendar.Contracts;
using HMS.Modules.Calendar.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Calendar.Domain;

public class EventTests
{
    private static readonly DateTime StartDate = new(2026, 8, 11, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EndDate = new(2026, 8, 11, 17, 0, 0, DateTimeKind.Utc);
    private static readonly Guid DepartmentId = Guid.NewGuid();

    [Fact]
    public void Create_SetsFieldsAndCreatedAudit()
    {
        var actorId = Guid.NewGuid();

        var calendarEvent = Event.Create("Fire Drill", "Annual fire safety drill", EventType.HospitalEvent, StartDate, EndDate, isAllDay: false, DepartmentId, actorId);

        calendarEvent.Title.Should().Be("Fire Drill");
        calendarEvent.Description.Should().Be("Annual fire safety drill");
        calendarEvent.EventType.Should().Be(EventType.HospitalEvent);
        calendarEvent.StartDate.Should().Be(StartDate);
        calendarEvent.EndDate.Should().Be(EndDate);
        calendarEvent.IsAllDay.Should().BeFalse();
        calendarEvent.DepartmentId.Should().Be(DepartmentId);
        calendarEvent.IsDeleted.Should().BeFalse();
        calendarEvent.CreatedBy.Should().Be(actorId);
        calendarEvent.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        calendarEvent.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsTitleAndDescription()
    {
        var calendarEvent = Event.Create("  Meeting  ", "  padded  ", EventType.Meeting, StartDate, EndDate, false, null, null);

        calendarEvent.Title.Should().Be("Meeting");
        calendarEvent.Description.Should().Be("padded");
    }

    [Fact]
    public void Create_WithNullDescriptionOrDepartment_LeavesThemNull()
    {
        var calendarEvent = Event.Create("Training", null, EventType.Training, StartDate, EndDate, false, null, null);

        calendarEvent.Description.Should().BeNull();
        calendarEvent.DepartmentId.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullOrWhitespaceTitle_Throws()
    {
        var act = () => Event.Create("   ", null, EventType.Other, StartDate, EndDate, false, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_UpdatesFieldsAndSetsUpdatedAudit()
    {
        var calendarEvent = Event.Create("Training", null, EventType.Training, StartDate, EndDate, false, null, null);
        var updatedBy = Guid.NewGuid();
        var newEnd = EndDate.AddHours(2);

        calendarEvent.Update("Training (Extended)", "Extra session added", EventType.Training, StartDate, newEnd, isAllDay: true, DepartmentId, updatedBy);

        calendarEvent.Title.Should().Be("Training (Extended)");
        calendarEvent.Description.Should().Be("Extra session added");
        calendarEvent.EndDate.Should().Be(newEnd);
        calendarEvent.IsAllDay.Should().BeTrue();
        calendarEvent.DepartmentId.Should().Be(DepartmentId);
        calendarEvent.UpdatedBy.Should().Be(updatedBy);
        calendarEvent.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAudit()
    {
        var calendarEvent = Event.Create("Maintenance Window", null, EventType.Maintenance, StartDate, EndDate, false, null, null);
        var deletedBy = Guid.NewGuid();

        calendarEvent.SoftDelete(deletedBy);

        calendarEvent.IsDeleted.Should().BeTrue();
        calendarEvent.DeletedBy.Should().Be(deletedBy);
        calendarEvent.DeletedAt.Should().NotBeNull();
    }
}
