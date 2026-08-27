using FluentAssertions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.HR.Domain;

public class AttendanceTests
{
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly DateOnly AttendanceDate = new(2026, 8, 27);

    [Fact]
    public void Create_SetsFieldsAndCreatedAudit()
    {
        var actorId = Guid.NewGuid();
        var checkIn = new DateTime(2026, 8, 27, 9, 0, 0, DateTimeKind.Utc);

        var attendance = Attendance.Create(EmployeeId, AttendanceDate, checkIn, null, AttendanceStatus.Present, "  on time  ", actorId);

        attendance.EmployeeId.Should().Be(EmployeeId);
        attendance.AttendanceDate.Should().Be(AttendanceDate);
        attendance.CheckInTime.Should().Be(checkIn);
        attendance.CheckOutTime.Should().BeNull();
        attendance.Status.Should().Be(AttendanceStatus.Present);
        attendance.Remarks.Should().Be("on time");
        attendance.CreatedBy.Should().Be(actorId);
        attendance.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Update_ReplacesMutableFieldsAndSetsUpdatedAudit()
    {
        var attendance = Attendance.Create(EmployeeId, AttendanceDate, null, null, AttendanceStatus.Absent, null, null);
        var updatedBy = Guid.NewGuid();
        var checkIn = new DateTime(2026, 8, 27, 9, 30, 0, DateTimeKind.Utc);

        attendance.Update(checkIn, null, AttendanceStatus.Late, "arrived late", updatedBy);

        attendance.CheckInTime.Should().Be(checkIn);
        attendance.Status.Should().Be(AttendanceStatus.Late);
        attendance.Remarks.Should().Be("arrived late");
        attendance.UpdatedBy.Should().Be(updatedBy);
        attendance.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordCheckIn_SetsCheckInTimeOnlyAndLeavesStatusUnchanged()
    {
        var attendance = Attendance.Create(EmployeeId, AttendanceDate, null, null, AttendanceStatus.HalfDay, null, null);
        var checkIn = new DateTime(2026, 8, 27, 13, 0, 0, DateTimeKind.Utc);

        attendance.RecordCheckIn(checkIn, null);

        attendance.CheckInTime.Should().Be(checkIn);
        attendance.Status.Should().Be(AttendanceStatus.HalfDay);
    }

    [Fact]
    public void RecordCheckOut_SetsCheckOutTimeOnly()
    {
        var attendance = Attendance.Create(EmployeeId, AttendanceDate, DateTime.UtcNow, null, AttendanceStatus.Present, null, null);
        var checkOut = new DateTime(2026, 8, 27, 18, 0, 0, DateTimeKind.Utc);

        attendance.RecordCheckOut(checkOut, null);

        attendance.CheckOutTime.Should().Be(checkOut);
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAudit()
    {
        var attendance = Attendance.Create(EmployeeId, AttendanceDate, null, null, AttendanceStatus.Absent, null, null);
        var deletedBy = Guid.NewGuid();

        attendance.SoftDelete(deletedBy);

        attendance.IsDeleted.Should().BeTrue();
        attendance.DeletedBy.Should().Be(deletedBy);
    }
}
