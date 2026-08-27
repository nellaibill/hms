using FluentAssertions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.HR.Domain;

public class LeaveRequestTests
{
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid LeaveTypeId = Guid.NewGuid();

    [Fact]
    public void Create_ComputesTotalDaysAsInclusiveDayCount()
    {
        var startDate = new DateOnly(2026, 9, 1);
        var endDate = new DateOnly(2026, 9, 3);

        var leaveRequest = LeaveRequest.Create(EmployeeId, LeaveTypeId, startDate, endDate, "Family event", null);

        leaveRequest.TotalDays.Should().Be(3);
        leaveRequest.Status.Should().Be(LeaveRequestStatus.Pending);
    }

    [Fact]
    public void Create_WithSameStartAndEndDate_ComputesOneDay()
    {
        var date = new DateOnly(2026, 9, 1);

        var leaveRequest = LeaveRequest.Create(EmployeeId, LeaveTypeId, date, date, "Single day", null);

        leaveRequest.TotalDays.Should().Be(1);
    }

    [Fact]
    public void Create_SetsCreatedAudit()
    {
        var actorId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 1);

        var leaveRequest = LeaveRequest.Create(EmployeeId, LeaveTypeId, date, date, "Reason", actorId);

        leaveRequest.CreatedBy.Should().Be(actorId);
        leaveRequest.EmployeeId.Should().Be(EmployeeId);
        leaveRequest.LeaveTypeId.Should().Be(LeaveTypeId);
        leaveRequest.Reason.Should().Be("Reason");
    }

    [Fact]
    public void Approve_SetsStatusApprovedAndApprovalAudit()
    {
        var date = new DateOnly(2026, 9, 1);
        var leaveRequest = LeaveRequest.Create(EmployeeId, LeaveTypeId, date, date, "Reason", null);
        var approverId = Guid.NewGuid();

        leaveRequest.Approve(approverId, "Approved, enjoy");

        leaveRequest.Status.Should().Be(LeaveRequestStatus.Approved);
        leaveRequest.ApprovedByUserId.Should().Be(approverId);
        leaveRequest.ApprovedAt.Should().NotBeNull();
        leaveRequest.DecisionNotes.Should().Be("Approved, enjoy");
    }

    [Fact]
    public void Reject_SetsStatusRejectedAndDecisionNotes()
    {
        var date = new DateOnly(2026, 9, 1);
        var leaveRequest = LeaveRequest.Create(EmployeeId, LeaveTypeId, date, date, "Reason", null);
        var approverId = Guid.NewGuid();

        leaveRequest.Reject(approverId, "Insufficient staffing that week");

        leaveRequest.Status.Should().Be(LeaveRequestStatus.Rejected);
        leaveRequest.ApprovedByUserId.Should().Be(approverId);
        leaveRequest.DecisionNotes.Should().Be("Insufficient staffing that week");
    }

    [Fact]
    public void Cancel_SetsStatusCancelled()
    {
        var date = new DateOnly(2026, 9, 1);
        var leaveRequest = LeaveRequest.Create(EmployeeId, LeaveTypeId, date, date, "Reason", null);
        var actorId = Guid.NewGuid();

        leaveRequest.Cancel(actorId);

        leaveRequest.Status.Should().Be(LeaveRequestStatus.Cancelled);
        leaveRequest.UpdatedBy.Should().Be(actorId);
    }
}
