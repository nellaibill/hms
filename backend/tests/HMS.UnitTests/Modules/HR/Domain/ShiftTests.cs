using FluentAssertions;
using HMS.Modules.HR.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.HR.Domain;

public class ShiftTests
{
    private static readonly TimeOnly Start = new(8, 0);
    private static readonly TimeOnly End = new(16, 0);

    [Fact]
    public void Create_SetsFieldsAndCreatedAudit()
    {
        var actorId = Guid.NewGuid();

        var shift = Shift.Create("morning", "Morning Shift", Start, End, 30, 10, false, true, actorId);

        shift.Code.Should().Be("MORNING");
        shift.Name.Should().Be("Morning Shift");
        shift.StartTime.Should().Be(Start);
        shift.EndTime.Should().Be(End);
        shift.BreakMinutes.Should().Be(30);
        shift.GraceMinutes.Should().Be(10);
        shift.IsNightShift.Should().BeFalse();
        shift.IsActive.Should().BeTrue();
        shift.IsDeleted.Should().BeFalse();
        shift.CreatedBy.Should().Be(actorId);
        shift.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        shift.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsNameAndNormalizesCodeToUppercase()
    {
        var shift = Shift.Create("  night  ", "  Night Shift  ", Start, End, 0, 0, true, true, null);

        shift.Code.Should().Be("NIGHT");
        shift.Name.Should().Be("Night Shift");
    }

    [Fact]
    public void Create_AllowsEndTimeBeforeStartTime_ForANightShiftCrossingMidnight()
    {
        var shift = Shift.Create("night", "Night Shift", new TimeOnly(22, 0), new TimeOnly(6, 0), 0, 0, true, true, null);

        shift.StartTime.Should().Be(new TimeOnly(22, 0));
        shift.EndTime.Should().Be(new TimeOnly(6, 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidCode_ThrowsArgumentException(string invalidCode)
    {
        var act = () => Shift.Create(invalidCode, "Morning Shift", Start, End, 0, 0, false, true, null);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ThrowsArgumentException(string invalidName)
    {
        var act = () => Shift.Create("morning", invalidName, Start, End, 0, 0, false, true, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_UpdatesFieldsAndSetsUpdatedAudit()
    {
        var shift = Shift.Create("morning", "Morning Shift", Start, End, 30, 10, false, true, null);
        var updatedBy = Guid.NewGuid();

        shift.Update("Morning (Revised)", new TimeOnly(9, 0), new TimeOnly(17, 0), 45, 15, true, false, updatedBy);

        shift.Name.Should().Be("Morning (Revised)");
        shift.StartTime.Should().Be(new TimeOnly(9, 0));
        shift.EndTime.Should().Be(new TimeOnly(17, 0));
        shift.BreakMinutes.Should().Be(45);
        shift.GraceMinutes.Should().Be(15);
        shift.IsNightShift.Should().BeTrue();
        shift.IsActive.Should().BeFalse();
        shift.UpdatedBy.Should().Be(updatedBy);
        shift.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_DoesNotChangeCode()
    {
        var shift = Shift.Create("morning", "Morning Shift", Start, End, 0, 0, false, true, null);

        shift.Update("Renamed", Start, End, 0, 0, false, true, null);

        shift.Code.Should().Be("MORNING");
    }

    [Fact]
    public void Update_WithInvalidName_ThrowsArgumentException()
    {
        var shift = Shift.Create("morning", "Morning Shift", Start, End, 0, 0, false, true, null);

        var act = () => shift.Update("", Start, End, 0, 0, false, true, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAudit()
    {
        var shift = Shift.Create("morning", "Morning Shift", Start, End, 0, 0, false, true, null);
        var deletedBy = Guid.NewGuid();

        shift.SoftDelete(deletedBy);

        shift.IsDeleted.Should().BeTrue();
        shift.DeletedBy.Should().Be(deletedBy);
        shift.DeletedAt.Should().NotBeNull();
    }
}
