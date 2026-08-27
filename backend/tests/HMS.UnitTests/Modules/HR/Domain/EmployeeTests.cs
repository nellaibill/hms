using FluentAssertions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.HR.Domain;

public class EmployeeTests
{
    private static readonly Guid DepartmentId = Guid.NewGuid();
    private static readonly Guid DesignationId = Guid.NewGuid();
    private static readonly DateOnly DateOfBirth = new(1990, 1, 1);
    private static readonly DateOnly JoiningDate = new(2024, 1, 1);

    private static Employee NewEmployee(Guid? reportingManagerId = null, Guid? userId = null, Guid? createdBy = null) => Employee.Create(
        "emp-001",
        "Ada",
        "Lovelace",
        Gender.Female,
        DateOfBirth,
        "555-0100",
        "ada@example.com",
        "1 Analytical Engine Way",
        "Charles Babbage",
        "555-0199",
        DepartmentId,
        DesignationId,
        EmployeeType.Permanent,
        JoiningDate,
        EmploymentStatus.Active,
        reportingManagerId,
        null,
        userId,
        true,
        createdBy);

    [Fact]
    public void Create_SetsFieldsAndCreatedAudit()
    {
        var actorId = Guid.NewGuid();

        var employee = NewEmployee(createdBy: actorId);

        employee.EmployeeCode.Should().Be("EMP-001");
        employee.FirstName.Should().Be("Ada");
        employee.LastName.Should().Be("Lovelace");
        employee.Gender.Should().Be(Gender.Female);
        employee.DateOfBirth.Should().Be(DateOfBirth);
        employee.DepartmentId.Should().Be(DepartmentId);
        employee.DesignationId.Should().Be(DesignationId);
        employee.EmployeeType.Should().Be(EmployeeType.Permanent);
        employee.JoiningDate.Should().Be(JoiningDate);
        employee.EmploymentStatus.Should().Be(EmploymentStatus.Active);
        employee.ReportingManagerId.Should().BeNull();
        employee.UserId.Should().BeNull();
        employee.IsActive.Should().BeTrue();
        employee.IsDeleted.Should().BeFalse();
        employee.CreatedBy.Should().Be(actorId);
        employee.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        employee.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_UppercasesAndTrimsEmployeeCode()
    {
        var employee = Employee.Create(
            "  emp-002  ",
            "Grace",
            "Hopper",
            Gender.Female,
            DateOfBirth,
            "555-0100",
            "grace@example.com",
            "Address",
            "Contact",
            "555-0199",
            DepartmentId,
            DesignationId,
            EmployeeType.Permanent,
            JoiningDate,
            EmploymentStatus.Active,
            null,
            null,
            null,
            true,
            null);

        employee.EmployeeCode.Should().Be("EMP-002");
    }

    [Fact]
    public void Create_WithReportingManagerAndUserId_SetsBoth()
    {
        var managerId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var employee = NewEmployee(reportingManagerId: managerId, userId: userId);

        employee.ReportingManagerId.Should().Be(managerId);
        employee.UserId.Should().Be(userId);
    }

    [Fact]
    public void Update_UpdatesFieldsAndSetsUpdatedAudit()
    {
        var employee = NewEmployee();
        var updatedBy = Guid.NewGuid();
        var newDepartmentId = Guid.NewGuid();
        var newDesignationId = Guid.NewGuid();

        employee.Update(
            "Ada",
            "Byron",
            Gender.Female,
            DateOfBirth,
            "555-0200",
            "ada.byron@example.com",
            "New address",
            "New contact",
            "555-0299",
            newDepartmentId,
            newDesignationId,
            EmployeeType.Contract,
            JoiningDate,
            EmploymentStatus.OnLeave,
            null,
            "https://example.com/photo.jpg",
            null,
            false,
            updatedBy);

        employee.LastName.Should().Be("Byron");
        employee.DepartmentId.Should().Be(newDepartmentId);
        employee.DesignationId.Should().Be(newDesignationId);
        employee.EmployeeType.Should().Be(EmployeeType.Contract);
        employee.EmploymentStatus.Should().Be(EmploymentStatus.OnLeave);
        employee.ProfilePhotoUrl.Should().Be("https://example.com/photo.jpg");
        employee.IsActive.Should().BeFalse();
        employee.UpdatedBy.Should().Be(updatedBy);
        employee.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_EmploymentStatusIsIndependentOfIsActive()
    {
        // An employee can be IsActive=true (the generic toggle) while EmploymentStatus is
        // OnLeave (the richer HR-domain status) at the same time — the two are deliberately
        // orthogonal (see Employee's class remarks).
        var employee = NewEmployee();

        employee.Update(
            employee.FirstName,
            employee.LastName,
            employee.Gender,
            employee.DateOfBirth,
            employee.Phone,
            employee.Email,
            employee.Address,
            employee.EmergencyContactName,
            employee.EmergencyContactPhone,
            employee.DepartmentId,
            employee.DesignationId,
            employee.EmployeeType,
            employee.JoiningDate,
            EmploymentStatus.OnLeave,
            null,
            null,
            null,
            true,
            null);

        employee.IsActive.Should().BeTrue();
        employee.EmploymentStatus.Should().Be(EmploymentStatus.OnLeave);
    }

    [Fact]
    public void Activate_SetsIsActiveTrueAndSetsUpdatedAudit()
    {
        var employee = NewEmployee();
        employee.Deactivate(null);
        var actorId = Guid.NewGuid();

        employee.Activate(actorId);

        employee.IsActive.Should().BeTrue();
        employee.UpdatedBy.Should().Be(actorId);
        employee.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalseAndSetsUpdatedAudit()
    {
        var employee = NewEmployee();
        var actorId = Guid.NewGuid();

        employee.Deactivate(actorId);

        employee.IsActive.Should().BeFalse();
        employee.UpdatedBy.Should().Be(actorId);
        employee.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAudit()
    {
        var employee = NewEmployee();
        var deletedBy = Guid.NewGuid();

        employee.SoftDelete(deletedBy);

        employee.IsDeleted.Should().BeTrue();
        employee.DeletedBy.Should().Be(deletedBy);
        employee.DeletedAt.Should().NotBeNull();
    }
}
