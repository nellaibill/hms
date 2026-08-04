using FluentAssertions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.HR.Domain;

public class ShiftAssignmentTests
{
    private static readonly Guid StaffId = Guid.NewGuid();
    private static readonly Guid DepartmentId = Guid.NewGuid();
    private static readonly Guid ShiftId = Guid.NewGuid();
    private static readonly DateOnly RosterDate = new(2026, 8, 4);

    [Fact]
    public void Create_SetsFieldsAndCreatedAudit()
    {
        var actorId = Guid.NewGuid();

        var assignment = ShiftAssignment.Create(StaffId, DepartmentId, ShiftId, RosterDate, AssignmentStatus.Scheduled, "First day", actorId);

        assignment.StaffId.Should().Be(StaffId);
        assignment.DepartmentId.Should().Be(DepartmentId);
        assignment.ShiftId.Should().Be(ShiftId);
        assignment.RosterDate.Should().Be(RosterDate);
        assignment.Status.Should().Be(AssignmentStatus.Scheduled);
        assignment.Remarks.Should().Be("First day");
        assignment.IsDeleted.Should().BeFalse();
        assignment.CreatedBy.Should().Be(actorId);
        assignment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        assignment.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsRemarks()
    {
        var assignment = ShiftAssignment.Create(StaffId, DepartmentId, ShiftId, RosterDate, AssignmentStatus.Scheduled, "  padded  ", null);

        assignment.Remarks.Should().Be("padded");
    }

    [Fact]
    public void Create_WithNullRemarks_LeavesRemarksNull()
    {
        var assignment = ShiftAssignment.Create(StaffId, DepartmentId, ShiftId, RosterDate, AssignmentStatus.Scheduled, null, null);

        assignment.Remarks.Should().BeNull();
    }

    [Fact]
    public void Create_AllowsTheSameStaffOnTheSameDateTwice_NoUniquenessEnforcedHere()
    {
        // Phase 2 explicitly defers overlap/duplicate-assignment rules to a later phase —
        // Domain has no invariant preventing two ShiftAssignment instances with identical
        // StaffId/RosterDate from both existing.
        var first = ShiftAssignment.Create(StaffId, DepartmentId, ShiftId, RosterDate, AssignmentStatus.Scheduled, null, null);
        var second = ShiftAssignment.Create(StaffId, DepartmentId, ShiftId, RosterDate, AssignmentStatus.Scheduled, null, null);

        first.Id.Should().NotBe(second.Id);
        first.StaffId.Should().Be(second.StaffId);
        first.RosterDate.Should().Be(second.RosterDate);
    }

    [Fact]
    public void Update_UpdatesFieldsAndSetsUpdatedAudit()
    {
        var assignment = ShiftAssignment.Create(StaffId, DepartmentId, ShiftId, RosterDate, AssignmentStatus.Scheduled, null, null);
        var updatedBy = Guid.NewGuid();
        var newShiftId = Guid.NewGuid();
        var newDate = new DateOnly(2026, 8, 5);

        assignment.Update(StaffId, DepartmentId, newShiftId, newDate, AssignmentStatus.Completed, "Done", updatedBy);

        assignment.ShiftId.Should().Be(newShiftId);
        assignment.RosterDate.Should().Be(newDate);
        assignment.Status.Should().Be(AssignmentStatus.Completed);
        assignment.Remarks.Should().Be("Done");
        assignment.UpdatedBy.Should().Be(updatedBy);
        assignment.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_TrimsRemarks()
    {
        var assignment = ShiftAssignment.Create(StaffId, DepartmentId, ShiftId, RosterDate, AssignmentStatus.Scheduled, null, null);

        assignment.Update(StaffId, DepartmentId, ShiftId, RosterDate, AssignmentStatus.Scheduled, "  padded  ", null);

        assignment.Remarks.Should().Be("padded");
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAudit()
    {
        var assignment = ShiftAssignment.Create(StaffId, DepartmentId, ShiftId, RosterDate, AssignmentStatus.Scheduled, null, null);
        var deletedBy = Guid.NewGuid();

        assignment.SoftDelete(deletedBy);

        assignment.IsDeleted.Should().BeTrue();
        assignment.DeletedBy.Should().Be(deletedBy);
        assignment.DeletedAt.Should().NotBeNull();
    }
}
