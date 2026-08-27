using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Application.Mapping;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using HMS.Modules.Identity.Application;
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
}

internal class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _repository;
    private readonly IDepartmentService _departmentService;
    private readonly IDesignationService _designationService;
    private readonly IUserService _userService;

    public EmployeeService(
        IEmployeeRepository repository,
        IDepartmentService departmentService,
        IDesignationService designationService,
        IUserService userService)
    {
        _repository = repository;
        _departmentService = departmentService;
        _designationService = designationService;
        _userService = userService;
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
            request.UserId,
            request.IsActive,
            actorId);

        await _repository.AddAsync(employee, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

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
            request.UserId,
            request.IsActive,
            actorId);

        await _repository.SaveChangesAsync(cancellationToken);

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
