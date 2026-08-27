using FluentAssertions;
using HMS.Modules.Notifications.Contracts;
using HMS.Modules.Notifications.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Notifications.Domain;

public class NotificationTemplateTests
{
    [Fact]
    public void Create_SetsFieldsAndCreatedAudit()
    {
        var actorId = Guid.NewGuid();

        var template = NotificationTemplate.Create("appointment.booked", NotificationChannel.Email, "Your appointment is booked", "Hello {{PatientName}}", actorId);

        template.TemplateKey.Should().Be("appointment.booked");
        template.Channel.Should().Be(NotificationChannel.Email);
        template.Subject.Should().Be("Your appointment is booked");
        template.BodyTemplate.Should().Be("Hello {{PatientName}}");
        template.IsActive.Should().BeTrue();
        template.CreatedBy.Should().Be(actorId);
        template.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        template.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_NonEmailChannel_AllowsNullSubject()
    {
        var template = NotificationTemplate.Create("appointment.reminder", NotificationChannel.Sms, null, "Reminder: your appointment is tomorrow", null);

        template.Subject.Should().BeNull();
    }

    [Fact]
    public void Create_EmailChannelWithoutSubject_Throws()
    {
        var act = () => NotificationTemplate.Create("appointment.booked", NotificationChannel.Email, null, "body", null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullOrWhitespaceTemplateKey_Throws()
    {
        var act = () => NotificationTemplate.Create("   ", NotificationChannel.InApp, null, "body", null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateContent_UpdatesFieldsAndSetsUpdatedAudit()
    {
        var template = NotificationTemplate.Create("task.assigned", NotificationChannel.InApp, null, "You were assigned {{TaskName}}", null);
        var updatedBy = Guid.NewGuid();

        template.UpdateContent(null, "You have a new task: {{TaskName}}", updatedBy);

        template.BodyTemplate.Should().Be("You have a new task: {{TaskName}}");
        template.UpdatedBy.Should().Be(updatedBy);
        template.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_ThenActivate_TogglesIsActiveAndSetsUpdatedAudit()
    {
        var template = NotificationTemplate.Create("task.assigned", NotificationChannel.InApp, null, "body", null);
        var actorId = Guid.NewGuid();

        template.Deactivate(actorId);
        template.IsActive.Should().BeFalse();
        template.UpdatedBy.Should().Be(actorId);

        template.Activate(actorId);
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_IsNoOp()
    {
        var template = NotificationTemplate.Create("task.assigned", NotificationChannel.InApp, null, "body", null);
        template.Deactivate(Guid.NewGuid());
        var updatedAtAfterFirstDeactivate = template.UpdatedAt;

        template.Deactivate(Guid.NewGuid());

        template.UpdatedAt.Should().Be(updatedAtAfterFirstDeactivate);
    }
}
