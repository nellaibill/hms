using HMS.Modules.HR.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Domain;

/// <summary>
/// A hospital staff member — the aggregate root for the Hospital HR Management MVP. Distinct
/// from both <c>identity.users</c> (a portal login) and <c>Masters.Consultant</c> (a doctor
/// name used for patient visits) — see docs/DecisionLog.md. Optionally links to an
/// <c>identity.users</c> row via <see cref="UserId"/> for employees who also have a portal
/// login; never required.
///
/// DepartmentId/DesignationId are plain Guid references with no database FK (Department lives
/// in Masters' schema, Designation likewise) — existence is checked only at the
/// Application/EmployeeService layer against Masters' public IDepartmentService/
/// IDesignationService, mirroring ShiftAssignment.DepartmentId's established cross-module
/// convention. ReportingManagerId is a same-table self-reference and does get a real database
/// FK (see EmployeeConfiguration) — Restrict delete, and an employee may not report to
/// themselves (checked in EmployeeService, not here — Domain accepts whatever value the
/// service already validated, same as every other cross-aggregate reference in this module).
///
/// IsActive is the generic Activate/Deactivate toggle every master-ish entity in this codebase
/// carries; EmploymentStatus is a separate, richer HR-domain lifecycle value. The two are
/// deliberately orthogonal — an employee can be IsActive=true and EmploymentStatus=OnLeave at
/// the same time.
/// </summary>
internal class Employee : Entity
{
    public string EmployeeCode { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public Gender Gender { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public string Phone { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public string EmergencyContactName { get; private set; } = null!;
    public string EmergencyContactPhone { get; private set; } = null!;
    public Guid DepartmentId { get; private set; }
    public Guid DesignationId { get; private set; }
    public EmployeeType EmployeeType { get; private set; }
    public DateOnly JoiningDate { get; private set; }
    public EmploymentStatus EmploymentStatus { get; private set; }
    public Guid? ReportingManagerId { get; private set; }
    public string? ProfilePhotoUrl { get; private set; }
    public Guid? UserId { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Required by EF Core materialization.
    private Employee()
    {
    }

    private Employee(
        Guid id,
        string employeeCode,
        string firstName,
        string lastName,
        Gender gender,
        DateOnly dateOfBirth,
        string phone,
        string email,
        string address,
        string emergencyContactName,
        string emergencyContactPhone,
        Guid departmentId,
        Guid designationId,
        EmployeeType employeeType,
        DateOnly joiningDate,
        EmploymentStatus employmentStatus,
        Guid? reportingManagerId,
        string? profilePhotoUrl,
        Guid? userId,
        bool isActive,
        Guid? createdBy)
        : base(id, createdBy)
    {
        EmployeeCode = employeeCode;
        FirstName = firstName;
        LastName = lastName;
        Gender = gender;
        DateOfBirth = dateOfBirth;
        Phone = phone;
        Email = email;
        Address = address;
        EmergencyContactName = emergencyContactName;
        EmergencyContactPhone = emergencyContactPhone;
        DepartmentId = departmentId;
        DesignationId = designationId;
        EmployeeType = employeeType;
        JoiningDate = joiningDate;
        EmploymentStatus = employmentStatus;
        ReportingManagerId = reportingManagerId;
        ProfilePhotoUrl = profilePhotoUrl;
        UserId = userId;
        IsActive = isActive;
    }

    public static Employee Create(
        string employeeCode,
        string firstName,
        string lastName,
        Gender gender,
        DateOnly dateOfBirth,
        string phone,
        string email,
        string address,
        string emergencyContactName,
        string emergencyContactPhone,
        Guid departmentId,
        Guid designationId,
        EmployeeType employeeType,
        DateOnly joiningDate,
        EmploymentStatus employmentStatus,
        Guid? reportingManagerId,
        string? profilePhotoUrl,
        Guid? userId,
        bool isActive,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(employeeCode, nameof(employeeCode));
        Guard.AgainstNullOrWhiteSpace(firstName, nameof(firstName));
        Guard.AgainstNullOrWhiteSpace(lastName, nameof(lastName));
        Guard.AgainstNullOrWhiteSpace(phone, nameof(phone));
        Guard.AgainstNullOrWhiteSpace(email, nameof(email));
        Guard.AgainstNullOrWhiteSpace(address, nameof(address));
        Guard.AgainstNullOrWhiteSpace(emergencyContactName, nameof(emergencyContactName));
        Guard.AgainstNullOrWhiteSpace(emergencyContactPhone, nameof(emergencyContactPhone));

        // Time-ordered UUID per docs/DatabaseArchitecture.md §4 — same convention every
        // other aggregate root in this codebase uses.
        return new Employee(
            Guid.CreateVersion7(),
            employeeCode.Trim().ToUpperInvariant(),
            firstName.Trim(),
            lastName.Trim(),
            gender,
            dateOfBirth,
            phone.Trim(),
            email.Trim(),
            address.Trim(),
            emergencyContactName.Trim(),
            emergencyContactPhone.Trim(),
            departmentId,
            designationId,
            employeeType,
            joiningDate,
            employmentStatus,
            reportingManagerId,
            profilePhotoUrl?.Trim(),
            userId,
            isActive,
            createdBy);
    }

    public void Update(
        string firstName,
        string lastName,
        Gender gender,
        DateOnly dateOfBirth,
        string phone,
        string email,
        string address,
        string emergencyContactName,
        string emergencyContactPhone,
        Guid departmentId,
        Guid designationId,
        EmployeeType employeeType,
        DateOnly joiningDate,
        EmploymentStatus employmentStatus,
        Guid? reportingManagerId,
        string? profilePhotoUrl,
        Guid? userId,
        bool isActive,
        Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(firstName, nameof(firstName));
        Guard.AgainstNullOrWhiteSpace(lastName, nameof(lastName));
        Guard.AgainstNullOrWhiteSpace(phone, nameof(phone));
        Guard.AgainstNullOrWhiteSpace(email, nameof(email));
        Guard.AgainstNullOrWhiteSpace(address, nameof(address));
        Guard.AgainstNullOrWhiteSpace(emergencyContactName, nameof(emergencyContactName));
        Guard.AgainstNullOrWhiteSpace(emergencyContactPhone, nameof(emergencyContactPhone));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Gender = gender;
        DateOfBirth = dateOfBirth;
        Phone = phone.Trim();
        Email = email.Trim();
        Address = address.Trim();
        EmergencyContactName = emergencyContactName.Trim();
        EmergencyContactPhone = emergencyContactPhone.Trim();
        DepartmentId = departmentId;
        DesignationId = designationId;
        EmployeeType = employeeType;
        JoiningDate = joiningDate;
        EmploymentStatus = employmentStatus;
        ReportingManagerId = reportingManagerId;
        ProfilePhotoUrl = profilePhotoUrl?.Trim();
        UserId = userId;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }

    /// <summary>The generic Activate/Deactivate toggle — independent of EmploymentStatus (see
    /// class remarks). Idempotent: calling Activate on an already-active employee is a no-op
    /// beyond touching the audit columns, matching Identity's User.Activate convention.</summary>
    public void Activate(Guid? updatedBy)
    {
        IsActive = true;
        MarkUpdated(updatedBy);
    }

    public void Deactivate(Guid? updatedBy)
    {
        IsActive = false;
        MarkUpdated(updatedBy);
    }
}
