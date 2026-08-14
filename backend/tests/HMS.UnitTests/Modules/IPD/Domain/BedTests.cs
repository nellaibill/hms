using FluentAssertions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Domain;

public class BedTests
{
    [Fact]
    public void Create_SetsFieldsAndCreatedAudit()
    {
        var wardId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var bed = Bed.Create(wardId, "b-101", BedType.Electric, BedStatus.Available, true, 1500m, actorId);

        bed.WardId.Should().Be(wardId);
        bed.BedNumber.Should().Be("B-101");
        bed.BedType.Should().Be(BedType.Electric);
        bed.Status.Should().Be(BedStatus.Available);
        bed.IsActive.Should().BeTrue();
        bed.CreatedBy.Should().Be(actorId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidBedNumber_ThrowsArgumentException(string invalidBedNumber)
    {
        var act = () => Bed.Create(Guid.NewGuid(), invalidBedNumber, BedType.Standard, BedStatus.Available, true, 1500m, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_UpdatesFieldsButNotWardOrBedNumber()
    {
        var wardId = Guid.NewGuid();
        var bed = Bed.Create(wardId, "b-101", BedType.Standard, BedStatus.Available, true, 1500m, null);
        var updatedBy = Guid.NewGuid();

        bed.Update(BedType.ICU, BedStatus.Maintenance, false, 1800m, updatedBy);

        bed.BedType.Should().Be(BedType.ICU);
        bed.Status.Should().Be(BedStatus.Maintenance);
        bed.IsActive.Should().BeFalse();
        bed.WardId.Should().Be(wardId);
        bed.BedNumber.Should().Be("B-101");
        bed.UpdatedBy.Should().Be(updatedBy);
    }

    [Fact]
    public void SetStatus_UpdatesStatusAndAudit()
    {
        var bed = Bed.Create(Guid.NewGuid(), "b-101", BedType.Standard, BedStatus.Available, true, 1500m, null);
        var updatedBy = Guid.NewGuid();

        bed.SetStatus(BedStatus.Occupied, updatedBy);

        bed.Status.Should().Be(BedStatus.Occupied);
        bed.UpdatedBy.Should().Be(updatedBy);
        bed.UpdatedAt.Should().NotBeNull();
    }
}
