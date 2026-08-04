using FluentAssertions;
using HMS.Modules.HR.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.HR.Domain;

public class WeeklyRosterTests
{
    private static readonly DateOnly WeekStartDate = new(2026, 8, 3);
    private static readonly Guid DepartmentId = Guid.NewGuid();

    [Fact]
    public void Create_SetsFieldsAndCreatedAudit()
    {
        var actorId = Guid.NewGuid();

        var roster = WeeklyRoster.Create(WeekStartDate, DepartmentId, false, null, actorId);

        roster.WeekStartDate.Should().Be(WeekStartDate);
        roster.DepartmentId.Should().Be(DepartmentId);
        roster.Published.Should().BeFalse();
        roster.PublishedDate.Should().BeNull();
        roster.IsDeleted.Should().BeFalse();
        roster.CreatedBy.Should().Be(actorId);
        roster.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        roster.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_AllowsPublishedTrueWithAPublishedDate()
    {
        var publishedDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        var roster = WeeklyRoster.Create(WeekStartDate, DepartmentId, true, publishedDate, null);

        roster.Published.Should().BeTrue();
        roster.PublishedDate.Should().Be(publishedDate);
    }

    [Fact]
    public void Update_UpdatesFieldsAndSetsUpdatedAudit()
    {
        var roster = WeeklyRoster.Create(WeekStartDate, DepartmentId, false, null, null);
        var updatedBy = Guid.NewGuid();
        var newDepartmentId = Guid.NewGuid();
        var newWeekStart = new DateOnly(2026, 8, 10);
        var publishedDate = new DateTime(2026, 8, 9, 0, 0, 0, DateTimeKind.Utc);

        roster.Update(newWeekStart, newDepartmentId, true, publishedDate, updatedBy);

        roster.WeekStartDate.Should().Be(newWeekStart);
        roster.DepartmentId.Should().Be(newDepartmentId);
        roster.Published.Should().BeTrue();
        roster.PublishedDate.Should().Be(publishedDate);
        roster.UpdatedBy.Should().Be(updatedBy);
        roster.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAudit()
    {
        var roster = WeeklyRoster.Create(WeekStartDate, DepartmentId, false, null, null);
        var deletedBy = Guid.NewGuid();

        roster.SoftDelete(deletedBy);

        roster.IsDeleted.Should().BeTrue();
        roster.DeletedBy.Should().Be(deletedBy);
        roster.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Publish_WhenNotYetPublished_SetsPublishedAndPublishedDateAndUpdatedAudit()
    {
        var roster = WeeklyRoster.Create(WeekStartDate, DepartmentId, false, null, null);
        var updatedBy = Guid.NewGuid();

        roster.Publish(updatedBy);

        roster.Published.Should().BeTrue();
        roster.PublishedDate.Should().NotBeNull();
        roster.PublishedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        roster.UpdatedBy.Should().Be(updatedBy);
        roster.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_IsIdempotent_DoesNotChangePublishedDateOrAudit()
    {
        var roster = WeeklyRoster.Create(WeekStartDate, DepartmentId, false, null, null);
        roster.Publish(Guid.NewGuid());
        var publishedDateAfterFirstPublish = roster.PublishedDate;
        var updatedAtAfterFirstPublish = roster.UpdatedAt;

        roster.Publish(Guid.NewGuid());

        roster.Published.Should().BeTrue();
        roster.PublishedDate.Should().Be(publishedDateAfterFirstPublish);
        roster.UpdatedAt.Should().Be(updatedAtAfterFirstPublish);
    }
}
