/** Mirrors HMS.Modules.HR.Contracts.HREnums — serialized as strings (JsonStringEnumConverter). */
export const ASSIGNMENT_STATUSES = ['Scheduled', 'Completed', 'Cancelled'] as const;
export type AssignmentStatus = (typeof ASSIGNMENT_STATUSES)[number];

/** Just the binary availability state — anything more specific is free text in `reason`. */
export const AVAILABILITY_STATUSES = ['Available', 'Unavailable'] as const;
export type AvailabilityStatus = (typeof AVAILABILITY_STATUSES)[number];

export const SWAP_REQUEST_STATUSES = ['Pending', 'Approved', 'Rejected', 'Cancelled'] as const;
export type SwapRequestStatus = (typeof SWAP_REQUEST_STATUSES)[number];

/** Employee's self-identified gender — a closed enum (not a Masters lookup table) per the
 * Hospital HR Management MVP spec. Mirrors HMS.Modules.HR.Contracts.Gender. */
export const EMPLOYEE_GENDERS = ['Male', 'Female', 'Other'] as const;
export type EmployeeGender = (typeof EMPLOYEE_GENDERS)[number];

/** The nature of an employee's engagement with the hospital. Mirrors
 * HMS.Modules.HR.Contracts.EmployeeType. */
export const EMPLOYEE_TYPES = ['Permanent', 'Contract', 'Intern', 'Consultant', 'PartTime'] as const;
export type EmployeeType = (typeof EMPLOYEE_TYPES)[number];

/** A richer HR-domain lifecycle status, independent of Employee.isActive (the generic
 * Activate/Deactivate toggle every master-ish entity in this codebase carries). Mirrors
 * HMS.Modules.HR.Contracts.EmploymentStatus. */
export const EMPLOYMENT_STATUSES = ['Active', 'OnLeave', 'Terminated', 'Resigned'] as const;
export type EmploymentStatus = (typeof EMPLOYMENT_STATUSES)[number];

/** A single day's attendance outcome for one employee. Mirrors
 * HMS.Modules.HR.Contracts.AttendanceStatus. */
export const ATTENDANCE_STATUSES = ['Present', 'Absent', 'Late', 'HalfDay', 'OnLeave'] as const;
export type AttendanceStatus = (typeof ATTENDANCE_STATUSES)[number];

/** The lifecycle state of a leave request. Mirrors
 * HMS.Modules.HR.Contracts.LeaveRequestStatus. */
export const LEAVE_REQUEST_STATUSES = ['Pending', 'Approved', 'Rejected', 'Cancelled'] as const;
export type LeaveRequestStatus = (typeof LEAVE_REQUEST_STATUSES)[number];
