using FluentAssertions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Domain;

public class WardTests
{
    [Fact]
    public void Create_SetsFieldsAndCreatedAudit()
    {
        var departmentId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var ward = Ward.Create("genmed", "General Medicine Ward A", departmentId, WardType.General, true, actorId);

        ward.Code.Should().Be("GENMED");
        ward.Name.Should().Be("General Medicine Ward A");
        ward.DepartmentId.Should().Be(departmentId);
        ward.WardType.Should().Be(WardType.General);
        ward.IsActive.Should().BeTrue();
        ward.CreatedBy.Should().Be(actorId);
        ward.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_TrimsNameAndNormalizesCodeToUppercase()
    {
        var ward = Ward.Create("  icu  ", "  ICU Ward  ", Guid.NewGuid(), WardType.ICU, true, null);

        ward.Code.Should().Be("ICU");
        ward.Name.Should().Be("ICU Ward");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidCode_ThrowsArgumentException(string invalidCode)
    {
        var act = () => Ward.Create(invalidCode, "General Ward", Guid.NewGuid(), WardType.General, true, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_UpdatesFieldsAndSetsUpdatedAudit()
    {
        var ward = Ward.Create("genmed", "General Medicine Ward A", Guid.NewGuid(), WardType.General, true, null);
        var newDepartmentId = Guid.NewGuid();
        var updatedBy = Guid.NewGuid();

        ward.Update("General Medicine Ward B", newDepartmentId, WardType.SemiPrivate, false, updatedBy);

        ward.Name.Should().Be("General Medicine Ward B");
        ward.DepartmentId.Should().Be(newDepartmentId);
        ward.WardType.Should().Be(WardType.SemiPrivate);
        ward.IsActive.Should().BeFalse();
        ward.UpdatedBy.Should().Be(updatedBy);
        ward.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_DoesNotChangeCode()
    {
        var ward = Ward.Create("genmed", "General Medicine Ward A", Guid.NewGuid(), WardType.General, true, null);

        ward.Update("Renamed", Guid.NewGuid(), WardType.General, true, null);

        ward.Code.Should().Be("GENMED");
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAudit()
    {
        var ward = Ward.Create("genmed", "General Medicine Ward A", Guid.NewGuid(), WardType.General, true, null);
        var deletedBy = Guid.NewGuid();

        ward.SoftDelete(deletedBy);

        ward.IsDeleted.Should().BeTrue();
        ward.DeletedBy.Should().Be(deletedBy);
        ward.DeletedAt.Should().NotBeNull();
    }
}
