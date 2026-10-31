using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Application.Mapping;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using HMS.Modules.Identity.Application;
using HMS.Modules.Identity.Contracts;
using HMS.Modules.Masters.Application;
using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Application;

/// <summary>
/// Public (not internal): EmployeesController — which ASP.NET Core requires to be a public
/// class with a public constructor for controller discovery/DI activation — takes this as a
/// constructor dependency (CS0051 otherwise). Interface and implementation share this file,
/// matching IShiftService.
/// </summary>
public interface IEmployeeService
{
    Task<Result<EmployeeResponse>> CreateAsync(CreateEmployeeRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<EmployeeResponse>> UpdateAsync(Guid id, UpdateEmployeeRequest request, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>The rich "profile" read — resolves DepartmentName/DesignationName/
    /// ReportingManagerName alongside the raw ids (see EmployeeResponse's remarks).</summary>
    Task<Result<EmployeeResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<EmployeeResponse>> GetPagedAsync(EmployeeListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<EmployeeResponse>> ActivateAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<EmployeeResponse>> DeactivateAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>Per active LeaveType: max days, days used this calendar year (sum of
    /// Approved LeaveRequests' TotalDays whose StartDate falls in the current year — the
    /// server's UTC "now" year, no fiscal-year config), and days remaining. No separate
    /// LeaveBalance table — computed on read every time, per the HR MVP spec.</summary>
    Task<Result<IReadOnlyList<EmployeeLeaveBalanceResponse>>> GetLeaveBalancesAsync(Guid employeeId, CancellationToken cancellationToken);
}

internal class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _repository;
    private readonly IDepartmentService _departmentService;
    private readonly IDesignationService _designationService;
    private readonly IUserService _userService;
    private readonly ILeaveTypeRepository _leaveTypeRepository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;

    public EmployeeService(
        IEmployeeRepository repository,
        IDepartmentService departmentService,
        IDesignationService designationService,
        IUserService userService,
        ILeaveTypeRepository leaveTypeRepository,
        ILeaveRequestRepository leaveRequestRepository)
    {
        _repository = repository;
        _departmentService = departmentService;
        _designationService = designationService;
        _userService = userService;
        _leaveTypeRepository = leaveTypeRepository;
        _leaveRequestRepository = leaveRequestRepository;
    }

    public async Task<Result<EmployeeResponse>> CreateAsync(CreateEmployeeRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.EmployeeCode.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<EmployeeResponse>.Failure(HRErrorCodes.DuplicateCode, $"Employee code '{request.EmployeeCode}' is already in use.");
        }

        var referenceError = await ValidateReferencesAsync(request.DepartmentId, request.DesignationId, request.ReportingManagerId, request.UserId, employeeId: null, cancellationToken);
        if (referenceError is not null)
        {
            return Result<EmployeeResponse>.Failure(referenceError.ErrorCode!, referenceError.Error!);
        }

        if (request.UserId.HasValue && await _repository.ExistsByUserIdAsync(request.UserId.Value, excludingId: null, cancellationToken))
        {
            return Result<EmployeeResponse>.Failure(HRErrorCodes.UserAlreadyLinked, "This login is already linked to another employee.");
        }

        // Last, once every employee-side check has passed — Identity saves the login straight
        // away, so nothing that could still reject the employee should come after it.
        var userId = request.UserId;
        Guid? createdLoginId = null;
        if (request.Login is not null)
        {
            var login = await CreateLoginAsync(request.Login, request.FirstName, request.LastName, request.Email, request.Phone, actorId, cancellationToken);
            if (!login.IsSuccess)
            {
                return Result<EmployeeResponse>.Failure(login.ErrorCode!, login.Error!);
            }

            userId = createdLoginId = login.Value!.Id;
        }

        var employee = Employee.Create(
            request.EmployeeCode,
            request.FirstName,
            request.LastName,
            request.Gender,
            request.DateOfBirth!.Value,
            request.Phone,
            request.Email,
            request.Address,
            request.EmergencyContactName,
            request.EmergencyContactPhone,
            request.DepartmentId,
            request.DesignationId,
            request.EmployeeType,
            request.JoiningDate!.Value,
            request.EmploymentStatus,
            request.ReportingManagerId,
            request.ProfilePhotoUrl,
            userId,
            request.IsActive,
            actorId);

        await _repository.AddAsync(employee, cancellationToken);
        await SaveChangesOrRemoveLoginAsync(createdLoginId, actorId, cancellationToken);

        return Result<EmployeeResponse>.Success(employee.ToResponse());
    }

    public async Task<Result<EmployeeResponse>> UpdateAsync(Guid id, UpdateEmployeeRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var employee = await _repository.GetByIdAsync(id, cancellationToken);
        if (employee is null)
        {
            return Result<EmployeeResponse>.Failure(HRErrorCodes.NotFound, $"Employee '{id}' was not found.");
        }

        var referenceError = await ValidateReferencesAsync(request.DepartmentId, request.DesignationId, request.ReportingManagerId, request.UserId, employeeId: id, cancellationToken);
        if (referenceError is not null)
        {
            return Result<EmployeeResponse>.Failure(referenceError.ErrorCode!, referenceError.Error!);
        }

        // Only when the link actually changes, so an employee saved before this rule can still
        // be edited as it stands.
        if (request.UserId.HasValue
            && request.UserId != employee.UserId
            && await _repository.ExistsByUserIdAsync(request.UserId.Value, excludingId: id, cancellationToken))
        {
            return Result<EmployeeResponse>.Failure(HRErrorCodes.UserAlreadyLinked, "This login is already linked to another employee.");
        }

        var userId = request.UserId;
        Guid? createdLoginId = null;
        if (request.Login is not null)
        {
            if (employee.UserId.HasValue)
            {
                return Result<EmployeeResponse>.Failure(HRErrorCodes.EmployeeAlreadyHasLogin, "This employee already has a login.");
            }

            var login = await CreateLoginAsync(request.Login, request.FirstName, request.LastName, request.Email, request.Phone, actorId, cancellationToken);
            if (!login.IsSuccess)
            {
                return Result<EmployeeResponse>.Failure(login.ErrorCode!, login.Error!);
            }

            userId = createdLoginId = login.Value!.Id;
        }

        employee.Update(
            request.FirstName,
            request.LastName,
            request.Gender,
            request.DateOfBirth!.Value,
            request.Phone,
            request.Email,
            request.Address,
            request.EmergencyContactName,
            request.EmergencyContactPhone,
            request.DepartmentId,
            request.DesignationId,
            request.EmployeeType,
            request.JoiningDate!.Value,
            request.EmploymentStatus,
            request.ReportingManagerId,
            request.ProfilePhotoUrl,
            userId,
            request.IsActive,
            actorId);

        await SaveChangesOrRemoveLoginAsync(createdLoginId, actorId, cancellationToken);

        return Result<EmployeeResponse>.Success(employee.ToResponse());
    }

    public async Task<Result<EmployeeResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var employee = await _repository.GetByIdAsync(id, cancellationToken);
        if (employee is null)
        {
            return Result<EmployeeResponse>.Failure(HRErrorCodes.NotFound, $"Employee '{id}' was not found.");
        }

        return Result<EmployeeResponse>.Success(await EnrichAsync(employee, cancellationToken));
    }

    public async Task<PagedResult<EmployeeResponse>> GetPagedAsync(EmployeeListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<EmployeeResponse>(items.Select(e => e.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var employee = await _repository.GetByIdAsync(id, cancellationToken);
        if (employee is null)
        {
            return Result.Failure(HRErrorCodes.NotFound, $"Employee '{id}' was not found.");
        }

        employee.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<EmployeeResponse>> ActivateAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var employee = await _repository.GetByIdAsync(id, cancellationToken);
        if (employee is null)
        {
            return Result<EmployeeResponse>.Failure(HRErrorCodes.NotFound, $"Employee '{id}' was not found.");
        }

        employee.Activate(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<EmployeeResponse>.Success(employee.ToResponse());
    }

    public async Task<Result<EmployeeResponse>> DeactivateAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var employee = await _repository.GetByIdAsync(id, cancellationToken);
        if (employee is null)
        {
            return Result<EmployeeResponse>.Failure(HRErrorCodes.NotFound, $"Employee '{id}' was not found.");
        }

        employee.Deactivate(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<EmployeeResponse>.Success(employee.ToResponse());
    }

    public async Task<Result<IReadOnlyList<EmployeeLeaveBalanceResponse>>> GetLeaveBalancesAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        if (!await _repository.ExistsAsync(employeeId, cancellationToken))
        {
            return Result<IReadOnlyList<EmployeeLeaveBalanceResponse>>.Failure(HRErrorCodes.NotFound, $"Employee '{employeeId}' was not found.");
        }

        var currentYear = DateTime.UtcNow.Year;
        var leaveTypes = await _leaveTypeRepository.GetActiveAsync(cancellationToken);

        var balances = new List<EmployeeLeaveBalanceResponse>();
        foreach (var leaveType in leaveTypes)
        {
            var usedDays = await _leaveRequestRepository.GetApprovedDaysAsync(employeeId, leaveType.Id, currentYear, cancellationToken);

            balances.Add(new EmployeeLeaveBalanceResponse
            {
                LeaveTypeId = leaveType.Id,
                LeaveTypeName = leaveType.Name,
                MaxDaysPerYear = leaveType.MaxDaysPerYear,
                UsedDays = usedDays,
                RemainingDays = leaveType.MaxDaysPerYear.HasValue ? leaveType.MaxDaysPerYear.Value - usedDays : null,
            });
        }

        return Result<IReadOnlyList<EmployeeLeaveBalanceResponse>>.Success(balances);
    }

    // Resolves DepartmentName/DesignationName/ReportingManagerName for the single-record
    // "profile" read — see EmployeeResponse's remarks for why this isn't done for every row
    // of a paged list.
    private async Task<EmployeeResponse> EnrichAsync(Employee employee, CancellationToken cancellationToken)
    {
        var response = employee.ToResponse();

        var departmentResult = await _departmentService.GetByIdAsync(employee.DepartmentId, cancellationToken);
        var designationResult = await _designationService.GetByIdAsync(employee.DesignationId, cancellationToken);

        string? reportingManagerName = null;
        if (employee.ReportingManagerId.HasValue)
        {
            var manager = await _repository.GetByIdAsync(employee.ReportingManagerId.Value, cancellationToken);
            if (manager is not null)
            {
                reportingManagerName = $"{manager.FirstName} {manager.LastName}";
            }
        }

        return response with
        {
            DepartmentName = departmentResult.IsSuccess ? departmentResult.Value!.Name : null,
            DesignationName = designationResult.IsSuccess ? designationResult.Value!.Name : null,
            ReportingManagerName = reportingManagerName,
        };
    }

    // The login's name, email and phone are the employee's own (ADR-108) — only username,
    // password and role come from the Login access card. Identity re-checks everything
    // (duplicate username/email, role exists) and its failure codes are passed straight back.
    private Task<Result<UserResponse>> CreateLoginAsync(
        EmployeeLoginRequest login,
        string firstName,
        string lastName,
        string email,
        string phone,
        Guid? actorId,
        CancellationToken cancellationToken)
        => _userService.CreateAsync(
            new CreateUserRequest
            {
                Username = login.Username.Trim(),
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                Email = email.Trim(),
                PhoneNumber = MobileNumber.ToTenDigits(phone),
                RoleId = login.RoleId,
                Password = login.Password,
            },
            actorId,
            cancellationToken);

    // Identity saved the new login in its own DbContext a moment ago — there's no transaction
    // spanning the two modules, so if the employee then fails to save, soft-delete the login
    // again rather than leave an account no employee points at.
    private async Task SaveChangesOrRemoveLoginAsync(Guid? createdLoginId, Guid? actorId, CancellationToken cancellationToken)
    {
        try
        {
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch when (createdLoginId.HasValue)
        {
            await _userService.DeleteAsync(createdLoginId.Value, actorId, CancellationToken.None);
            throw;
        }
    }

    // Every cross-aggregate reference check a Create/Update call needs, run together since
    // every caller needs all of them. DepartmentId/DesignationId are cross-module (Masters);
    // ReportingManagerId is same-module self-reference; UserId is cross-module (Identity) and
    // only checked when actually supplied (always optional).
    private async Task<Result?> ValidateReferencesAsync(
        Guid departmentId,
        Guid designationId,
        Guid? reportingManagerId,
        Guid? userId,
        Guid? employeeId,
        CancellationToken cancellationToken)
    {
        if (!(await _departmentService.GetByIdAsync(departmentId, cancellationToken)).IsSuccess)
        {
            return Result.Failure(HRErrorCodes.InvalidDepartment, $"Department '{departmentId}' was not found.");
        }

        if (!(await _designationService.GetByIdAsync(designationId, cancellationToken)).IsSuccess)
        {
            return Result.Failure(HRErrorCodes.InvalidDesignation, $"Designation '{designationId}' was not found.");
        }

        if (reportingManagerId.HasValue)
        {
            if (reportingManagerId.Value == employeeId)
            {
                return Result.Failure(HRErrorCodes.InvalidReportingManager, "An employee may not report to themselves.");
            }

            if (!await _repository.ExistsAsync(reportingManagerId.Value, cancellationToken))
            {
                return Result.Failure(HRErrorCodes.InvalidReportingManager, $"Reporting manager '{reportingManagerId}' was not found.");
            }
        }

        if (userId.HasValue && !(await _userService.GetByIdAsync(userId.Value, cancellationToken)).IsSuccess)
        {
            return Result.Failure(HRErrorCodes.InvalidUser, $"User '{userId}' was not found.");
        }

        return null;
    }
}
