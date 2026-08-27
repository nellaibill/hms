using FluentAssertions;
using HMS.Modules.Notifications.Contracts;
using HMS.Modules.Notifications.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Notifications.Domain;

public class NotificationTests
{
    [Fact]
    public void Create_SetsFieldsAndCreatedAudit()
    {
        var actorId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();

        var notification = Notification.Create(
            "appointment.booked",
            "Appointment",
            "Appointment booked",
            "Your appointment with Dr. Revathi is confirmed for tomorrow.",
            "Appointments",
            "Appointment",
            appointmentId,
            NotificationSeverity.Normal,
            actorId);

        notification.TemplateKey.Should().Be("appointment.booked");
        notification.Category.Should().Be("appointment");
        notification.Title.Should().Be("Appointment booked");
        notification.SourceModule.Should().Be("Appointments");
        notification.SourceEntityType.Should().Be("Appointment");
        notification.SourceEntityId.Should().Be(appointmentId);
        notification.Severity.Should().Be(NotificationSeverity.Normal);
        notification.CreatedBy.Should().Be(actorId);
        notification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithoutSourceEntity_LeavesBothNull()
    {
        var notification = Notification.Create(
            "emergency.broadcast",
            "Emergency",
            "Code Blue",
            "All available staff to Ward 4 immediately.",
            "Staff",
            null,
            null,
            NotificationSeverity.Emergency,
            null);

        notification.SourceEntityType.Should().BeNull();
        notification.SourceEntityId.Should().BeNull();
    }

    [Fact]
    public void Create_WithSourceEntityTypeButNoId_Throws()
    {
        var act = () => Notification.Create(
            "task.assigned",
            "Task",
            "New task",
            "body",
            "HR",
            "Task",
            sourceEntityId: null,
            NotificationSeverity.Normal,
            null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithSourceEntityIdButNoType_Throws()
    {
        var act = () => Notification.Create(
            "task.assigned",
            "Task",
            "New task",
            "body",
            "HR",
            sourceEntityType: null,
            Guid.NewGuid(),
            NotificationSeverity.Normal,
            null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullOrWhitespaceTitle_Throws()
    {
        var act = () => Notification.Create("k", "cat", "   ", "body", "Module", null, null, NotificationSeverity.Normal, null);

        act.Should().Throw<ArgumentException>();
    }
}
