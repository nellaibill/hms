using FluentAssertions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.HR.Domain;

public class ShiftSwapRequestTests
{
    private static readonly Guid RequestedByStaffId = Guid.NewGuid();
    private static readonly Guid RequestedToStaffId = Guid.NewGuid();
    private static readonly Guid CurrentShiftAssignmentId = Guid.NewGuid();
    private static readonly Guid RequestedShiftAssignmentId = Guid.NewGuid();
    private static readonly DateTime RequestedDate = new(2026, 8, 3, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_SetsFieldsAndCreatedAudit()
    {
        var actorId = Guid.NewGuid();

        var swapRequest = ShiftSwapRequest.Create(
            RequestedByStaffId, RequestedToStaffId, CurrentShiftAssignmentId, RequestedShiftAssignmentId,
            SwapRequestStatus.Pending, RequestedDate, null, null, "Please swap", actorId);

        swapRequest.RequestedByStaffId.Should().Be(RequestedByStaffId);
        swapRequest.RequestedToStaffId.Should().Be(RequestedToStaffId);
        swapRequest.CurrentShiftAssignmentId.Should().Be(CurrentShiftAssignmentId);
        swapRequest.RequestedShiftAssignmentId.Should().Be(RequestedShiftAssignmentId);
        swapRequest.Status.Should().Be(SwapRequestStatus.Pending);
        swapRequest.RequestedDate.Should().Be(RequestedDate);
        swapRequest.ApprovedDate.Should().BeNull();
        swapRequest.ApprovedBy.Should().BeNull();
        swapRequest.Remarks.Should().Be("Please swap");
        swapRequest.IsDeleted.Should().BeFalse();
        swapRequest.CreatedBy.Should().Be(actorId);
        swapRequest.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        swapRequest.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsRemarks()
    {
        var swapRequest = ShiftSwapRequest.Create(
            RequestedByStaffId, RequestedToStaffId, CurrentShiftAssignmentId, RequestedShiftAssignmentId,
            SwapRequestStatus.Pending, RequestedDate, null, null, "  padded  ", null);

        swapRequest.Remarks.Should().Be("padded");
    }

    [Fact]
    public void Create_AllowsRequestedByAndRequestedToToBeTheSameStaffMember_NoRuleAgainstSelfSwap()
    {
        var swapRequest = ShiftSwapRequest.Create(
            RequestedByStaffId, RequestedByStaffId, CurrentShiftAssignmentId, RequestedShiftAssignmentId,
            SwapRequestStatus.Pending, RequestedDate, null, null, null, null);

        swapRequest.RequestedByStaffId.Should().Be(swapRequest.RequestedToStaffId);
    }

    [Fact]
    public void Create_AllowsApprovedFieldsToBeSetEvenWhenStatusIsPending_NoConsistencyRule()
    {
        var approvedDate = new DateTime(2026, 8, 4, 0, 0, 0, DateTimeKind.Utc);
        var approvedBy = Guid.NewGuid();

        var swapRequest = ShiftSwapRequest.Create(
            RequestedByStaffId, RequestedToStaffId, CurrentShiftAssignmentId, RequestedShiftAssignmentId,
            SwapRequestStatus.Pending, RequestedDate, approvedDate, approvedBy, null, null);

        swapRequest.Status.Should().Be(SwapRequestStatus.Pending);
        swapRequest.ApprovedDate.Should().Be(approvedDate);
        swapRequest.ApprovedBy.Should().Be(approvedBy);
    }

    [Fact]
    public void Update_UpdatesFieldsAndSetsUpdatedAudit()
    {
        var swapRequest = ShiftSwapRequest.Create(
            RequestedByStaffId, RequestedToStaffId, CurrentShiftAssignmentId, RequestedShiftAssignmentId,
            SwapRequestStatus.Pending, RequestedDate, null, null, null, null);
        var updatedBy = Guid.NewGuid();
        var approvedDate = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc);
        var approvedBy = Guid.NewGuid();

        swapRequest.Update(
            RequestedByStaffId, RequestedToStaffId, CurrentShiftAssignmentId, RequestedShiftAssignmentId,
            SwapRequestStatus.Approved, RequestedDate, approvedDate, approvedBy, "Approved", updatedBy);

        swapRequest.Status.Should().Be(SwapRequestStatus.Approved);
        swapRequest.ApprovedDate.Should().Be(approvedDate);
        swapRequest.ApprovedBy.Should().Be(approvedBy);
        swapRequest.Remarks.Should().Be("Approved");
        swapRequest.UpdatedBy.Should().Be(updatedBy);
        swapRequest.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAudit()
    {
        var swapRequest = ShiftSwapRequest.Create(
            RequestedByStaffId, RequestedToStaffId, CurrentShiftAssignmentId, RequestedShiftAssignmentId,
            SwapRequestStatus.Pending, RequestedDate, null, null, null, null);
        var deletedBy = Guid.NewGuid();

        swapRequest.SoftDelete(deletedBy);

        swapRequest.IsDeleted.Should().BeTrue();
        swapRequest.DeletedBy.Should().Be(deletedBy);
        swapRequest.DeletedAt.Should().NotBeNull();
    }
}
