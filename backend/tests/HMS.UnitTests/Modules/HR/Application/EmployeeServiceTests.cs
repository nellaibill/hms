using FluentAssertions;
using HMS.Modules.HR.Application;
using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using HMS.Modules.Identity.Application;
using HMS.Modules.Identity.Contracts;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Shared.Kernel;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.HR.Application;

public class EmployeeServiceTests
{
    private static readonly DateOnly DateOfBirth = new(1990, 1, 1);
    private static readonly DateOnly JoiningDate = new(2024, 1, 1);

    private readonly IEmployeeRepository _repository = Substitute.For<IEmployeeRepository>();
    private readonly IDepartmentService _departmentService = Substitute.For<IDepartmentService>();
    private readonly IDesignationService _designationService = Substitute.For<IDesignationService>();
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly EmployeeService _sut;

    public EmployeeServiceTests()
    {
        _sut = new EmployeeService(_repository, _departmentService, _designationService, _userService);

        _departmentService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DepartmentResponse>.Success(new DepartmentResponse { Name = "Cardiology" }));
        _designationService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DesignationResponse>.Success(new DesignationResponse { Name = "Nurse" }));
        _userService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<UserResponse>.Success(new UserResponse()));
    }

    private static CreateEmployeeRequest NewCreateRequest() => new()
    {
        EmployeeCode = "EMP-100",
        FirstName = "Ada",
        LastName = "Lovelace",
        Gender = Gender.Female,
        DateOfBirth = DateOfBirth,
        Phone = "555-0100",
        Email = "ada@example.com",
        Address = "1 Analytical Engine Way",
        EmergencyContactName = "Charles Babbage",
        EmergencyContactPhone = "555-0199",
        DepartmentId = Guid.NewGuid(),
        DesignationId = Guid.NewGuid(),
        EmployeeType = EmployeeType.Permanent,
        JoiningDate = JoiningDate,
        EmploymentStatus = EmploymentStatus.Active,
        IsActive = true,
    };

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesEmployeeAndReturnsSuccess()
    {
        var request = NewCreateRequest();

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EmployeeCode.Should().Be("EMP-100");
        await _repository.Received(1).AddAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmployeeCode_ReturnsDuplicateCodeFailure()
    {
        _repository.ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.DuplicateCode);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenDepartmentDoesNotExist_ReturnsInvalidDepartmentFailure()
    {
        _departmentService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DepartmentResponse>.Failure("MASTERS.NOT_FOUND", "not found"));

        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.InvalidDepartment);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenDesignationDoesNotExist_ReturnsInvalidDesignationFailure()
    {
        _designationService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DesignationResponse>.Failure("MASTERS.NOT_FOUND", "not found"));

        var result = await _sut.CreateAsync(NewCreateRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.InvalidDesignation);
    }

    [Fact]
    public async Task CreateAsync_WhenReportingManagerDoesNotExist_ReturnsInvalidReportingManagerFailure()
    {
        _repository.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        var request = NewCreateRequest() with { ReportingManagerId = Guid.NewGuid() };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.InvalidReportingManager);
    }

    [Fact]
    public async Task CreateAsync_WhenUserIdDoesNotExist_ReturnsInvalidUserFailure()
    {
        _userService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<UserResponse>.Failure("IDENTITY.USER_NOT_FOUND", "not found"));
        var request = NewCreateRequest() with { UserId = Guid.NewGuid() };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.InvalidUser);
    }

    [Fact]
    public async Task UpdateAsync_WhenReportingManagerIsSelf_ReturnsInvalidReportingManagerFailure()
    {
        var employee = Employee.Create("EMP-1", "A", "B", Gender.Male, DateOfBirth, "p", "e@example.com", "addr", "c", "cp", Guid.NewGuid(), Guid.NewGuid(), EmployeeType.Permanent, JoiningDate, EmploymentStatus.Active, null, null, null, true, null);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var request = new UpdateEmployeeRequest
        {
            FirstName = "A",
            LastName = "B",
            Gender = Gender.Male,
            DateOfBirth = DateOfBirth,
            Phone = "p",
            Email = "e@example.com",
            Address = "addr",
            EmergencyContactName = "c",
            EmergencyContactPhone = "cp",
            DepartmentId = employee.DepartmentId,
            DesignationId = employee.DesignationId,
            EmployeeType = EmployeeType.Permanent,
            JoiningDate = JoiningDate,
            EmploymentStatus = EmploymentStatus.Active,
            ReportingManagerId = employee.Id,
            IsActive = true,
        };

        var result = await _sut.UpdateAsync(employee.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.InvalidReportingManager);
    }

    [Fact]
    public async Task UpdateAsync_WhenEmployeeNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateEmployeeRequest(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_EnrichesWithDepartmentAndDesignationNames()
    {
        var employee = Employee.Create("EMP-1", "A", "B", Gender.Male, DateOfBirth, "p", "e@example.com", "addr", "c", "cp", Guid.NewGuid(), Guid.NewGuid(), EmployeeType.Permanent, JoiningDate, EmploymentStatus.Active, null, null, null, true, null);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await _sut.GetByIdAsync(employee.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DepartmentName.Should().Be("Cardiology");
        result.Value.DesignationName.Should().Be("Nurse");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(HRErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPagedResultWithoutEnrichment()
    {
        var employee = Employee.Create("EMP-1", "A", "B", Gender.Male, DateOfBirth, "p", "e@example.com", "addr", "c", "cp", Guid.NewGuid(), Guid.NewGuid(), EmployeeType.Permanent, JoiningDate, EmploymentStatus.Active, null, null, null, true, null);
        _repository.GetPagedAsync(Arg.Any<EmployeeListQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<Employee> { employee }, 1));

        var result = await _sut.GetPagedAsync(new EmployeeListQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle(e => e.Id == employee.Id);
        result.Items[0].DepartmentName.Should().BeNull();
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ActivateAsync_WhenFound_ActivatesAndReturnsSuccess()
    {
        var employee = Employee.Create("EMP-1", "A", "B", Gender.Male, DateOfBirth, "p", "e@example.com", "addr", "c", "cp", Guid.NewGuid(), Guid.NewGuid(), EmployeeType.Permanent, JoiningDate, EmploymentStatus.Active, null, null, null, false, null);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await _sut.ActivateAsync(employee.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.IsActive.Should().BeTrue();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateAsync_WhenFound_DeactivatesAndReturnsSuccess()
    {
        var employee = Employee.Create("EMP-1", "A", "B", Gender.Male, DateOfBirth, "p", "e@example.com", "addr", "c", "cp", Guid.NewGuid(), Guid.NewGuid(), EmployeeType.Permanent, JoiningDate, EmploymentStatus.Active, null, null, null, true, null);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await _sut.DeactivateAsync(employee.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_SoftDeletesAndReturnsSuccess()
    {
        var employee = Employee.Create("EMP-1", "A", "B", Gender.Male, DateOfBirth, "p", "e@example.com", "addr", "c", "cp", Guid.NewGuid(), Guid.NewGuid(), EmployeeType.Permanent, JoiningDate, EmploymentStatus.Active, null, null, null, true, null);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await _sut.DeleteAsync(employee.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.IsDeleted.Should().BeTrue();
    }
}
