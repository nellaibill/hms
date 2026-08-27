using FluentAssertions;
using HMS.Modules.HR.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.HR.Domain;

public class LeaveTypeTests
{
    [Fact]
    public void Create_SetsFieldsAndCreatedAudit()
    {
        var actorId = Guid.NewGuid();

        var leaveType = LeaveType.Create("cl", "Casual Leave", 12, true, true, actorId);

        leaveType.Code.Should().Be("CL");
        leaveType.Name.Should().Be("Casual Leave");
        leaveType.MaxDaysPerYear.Should().Be(12);
        leaveType.IsPaid.Should().BeTrue();
        leaveType.IsActive.Should().BeTrue();
        leaveType.CreatedBy.Should().Be(actorId);
    }

    [Fact]
    public void Create_WithNullMaxDaysPerYear_MeansUnlimited()
    {
        var leaveType = LeaveType.Create("ul", "Unlimited Leave", null, false, true, null);

        leaveType.MaxDaysPerYear.Should().BeNull();
    }

    [Fact]
    public void Update_UpdatesFieldsAndSetsUpdatedAudit()
    {
        var leaveType = LeaveType.Create("cl", "Casual Leave", 12, true, true, null);
        var updatedBy = Guid.NewGuid();

        leaveType.Update("Casual Leave (Updated)", 15, false, false, updatedBy);

        leaveType.Name.Should().Be("Casual Leave (Updated)");
        leaveType.MaxDaysPerYear.Should().Be(15);
        leaveType.IsPaid.Should().BeFalse();
        leaveType.IsActive.Should().BeFalse();
        leaveType.UpdatedBy.Should().Be(updatedBy);
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var leaveType = LeaveType.Create("cl", "Casual Leave", 12, true, false, null);

        leaveType.Activate(null);

        leaveType.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var leaveType = LeaveType.Create("cl", "Casual Leave", 12, true, true, null);

        leaveType.Deactivate(null);

        leaveType.IsActive.Should().BeFalse();
    }
}
