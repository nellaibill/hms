using FluentAssertions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.HR.Domain;

public class StaffAvailabilityTests
{
    private static readonly Guid StaffId = Guid.NewGuid();
    private static readonly DateOnly StartDate = new(2026, 8, 3);
    private static readonly DateOnly EndDate = new(2026, 8, 10);

    [Fact]
    public void Create_SetsFieldsAndCreatedAudit()
    {
        var actorId = Guid.NewGuid();

        var availability = StaffAvailability.Create(StaffId, StartDate, EndDate, AvailabilityStatus.Unavailable, "Conference", actorId);

        availability.StaffId.Should().Be(StaffId);
        availability.StartDate.Should().Be(StartDate);
        availability.EndDate.Should().Be(EndDate);
        availability.AvailabilityStatus.Should().Be(AvailabilityStatus.Unavailable);
        availability.Reason.Should().Be("Conference");
        availability.IsDeleted.Should().BeFalse();
        availability.CreatedBy.Should().Be(actorId);
        availability.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        availability.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsReason()
    {
        var availability = StaffAvailability.Create(StaffId, StartDate, EndDate, AvailabilityStatus.Unavailable, "  Medical Leave  ", null);

        availability.Reason.Should().Be("Medical Leave");
    }

    [Fact]
    public void Create_WithNullReason_LeavesReasonNull()
    {
        var availability = StaffAvailability.Create(StaffId, StartDate, EndDate, AvailabilityStatus.Available, null, null);

        availability.Reason.Should().BeNull();
    }

    [Fact]
    public void Create_AllowsEndDateBeforeStartDate_NoDateOrderCheck()
    {
        // Explicitly out of scope for this phase.
        var availability = StaffAvailability.Create(StaffId, EndDate, StartDate, AvailabilityStatus.Available, null, null);

        availability.StartDate.Should().Be(EndDate);
        availability.EndDate.Should().Be(StartDate);
    }

    [Fact]
    public void Update_UpdatesFieldsAndSetsUpdatedAudit()
    {
        var availability = StaffAvailability.Create(StaffId, StartDate, EndDate, AvailabilityStatus.Available, null, null);
        var updatedBy = Guid.NewGuid();
        var newStaffId = Guid.NewGuid();
        var newStart = new DateOnly(2026, 9, 1);
        var newEnd = new DateOnly(2026, 9, 5);

        availability.Update(newStaffId, newStart, newEnd, AvailabilityStatus.Unavailable, "Training", updatedBy);

        availability.StaffId.Should().Be(newStaffId);
        availability.StartDate.Should().Be(newStart);
        availability.EndDate.Should().Be(newEnd);
        availability.AvailabilityStatus.Should().Be(AvailabilityStatus.Unavailable);
        availability.Reason.Should().Be("Training");
        availability.UpdatedBy.Should().Be(updatedBy);
        availability.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAudit()
    {
        var availability = StaffAvailability.Create(StaffId, StartDate, EndDate, AvailabilityStatus.Available, null, null);
        var deletedBy = Guid.NewGuid();

        availability.SoftDelete(deletedBy);

        availability.IsDeleted.Should().BeTrue();
        availability.DeletedBy.Should().Be(deletedBy);
        availability.DeletedAt.Should().NotBeNull();
    }
}
