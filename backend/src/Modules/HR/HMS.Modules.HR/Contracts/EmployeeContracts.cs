using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Contracts;

public record CreateEmployeeRequest
{
    public string EmployeeCode { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public Gender Gender { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string EmergencyContactName { get; init; } = string.Empty;
    public string EmergencyContactPhone { get; init; } = string.Empty;
    public Guid DepartmentId { get; init; }
    public Guid DesignationId { get; init; }
    public EmployeeType EmployeeType { get; init; }
    public DateOnly? JoiningDate { get; init; }
    public EmploymentStatus EmploymentStatus { get; init; } = EmploymentStatus.Active;
    public Guid? ReportingManagerId { get; init; }
    public string? ProfilePhotoUrl { get; init; }
    public Guid? UserId { get; init; }
    public bool IsActive { get; init; } = true;
}

// EmployeeCode is intentionally absent — a natural-key field, protected from change after
// creation (mirrors Department.Code/Shift.Code).
public record UpdateEmployeeRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public Gender Gender { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string EmergencyContactName { get; init; } = string.Empty;
    public string EmergencyContactPhone { get; init; } = string.Empty;
    public Guid DepartmentId { get; init; }
    public Guid DesignationId { get; init; }
    public EmployeeType EmployeeType { get; init; }
    public DateOnly? JoiningDate { get; init; }
    public EmploymentStatus EmploymentStatus { get; init; }
    public Guid? ReportingManagerId { get; init; }
    public string? ProfilePhotoUrl { get; init; }
    public Guid? UserId { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// One shape for both the paged list and the single-record "profile" GET. DepartmentName/
/// DesignationName/ReportingManagerName are populated only by GetByIdAsync (a single cheap
/// extra lookup each against Masters' public services / this module's own repository) — left
/// null on paged list results, where enriching every row would mean N extra cross-module
/// calls per page. The frontend already needs Department/Designation dropdown data for list
/// filters, so resolving names client-side for the list view is a non-issue; see
/// docs/DecisionLog.md ADR-036 for the reasoning.
/// </summary>
public record EmployeeResponse
{
    public Guid Id { get; init; }
    public string EmployeeCode { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public Gender Gender { get; init; }
    public DateOnly DateOfBirth { get; init; }
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string EmergencyContactName { get; init; } = string.Empty;
    public string EmergencyContactPhone { get; init; } = string.Empty;
    public Guid DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public Guid DesignationId { get; init; }
    public string? DesignationName { get; init; }
    public EmployeeType EmployeeType { get; init; }
    public DateOnly JoiningDate { get; init; }
    public EmploymentStatus EmploymentStatus { get; init; }
    public Guid? ReportingManagerId { get; init; }
    public string? ReportingManagerName { get; init; }
    public string? ProfilePhotoUrl { get; init; }
    public Guid? UserId { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>Search spans EmployeeCode/FirstName/LastName/Email (see EmployeeRepository).</summary>
public class EmployeeListQuery : PagedRequest
{
    public Guid? DepartmentId { get; set; }
    public Guid? DesignationId { get; set; }
    public EmployeeType? EmployeeType { get; set; }
    public EmploymentStatus? EmploymentStatus { get; set; }
    public bool? IsActive { get; set; }
}

public record EmployeeLeaveBalanceResponse
{
    public Guid LeaveTypeId { get; init; }
    public string LeaveTypeName { get; init; } = string.Empty;
    public int? MaxDaysPerYear { get; init; }
    public int UsedDays { get; init; }

    /// <summary>Null when <see cref="MaxDaysPerYear"/> is null (unlimited) — there is no
    /// meaningful "remaining" figure for an unlimited leave type.</summary>
    public int? RemainingDays { get; init; }
}
