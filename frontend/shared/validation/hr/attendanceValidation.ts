import { z } from 'zod';
import { ATTENDANCE_STATUSES } from '../../enums/hr';

/**
 * Mirrors HMS.Modules.HR.Application.Validators.CreateAttendanceRequestValidator /
 * UpdateAttendanceRequestValidator — only EmployeeId/AttendanceDate (create-only, the
 * natural key, immutable after creation) and Remarks' max length are enforced server-side;
 * Status has no backend rule beyond its type, but is required client-side since the form
 * always needs one selected (client-side convenience only, the backend remains authoritative
 * — docs/ApiStandards.md §7, docs/FrontendArchitecture.md §9).
 */
const attendanceCommonSchema = {
  checkInTime: z.string().trim().optional().or(z.literal('')),
  checkOutTime: z.string().trim().optional().or(z.literal('')),
  status: z.enum(ATTENDANCE_STATUSES, { message: 'Status is required' }),
  remarks: z.string().trim().max(500).optional().or(z.literal('')),
};

export const createAttendanceSchema = z.object({
  employeeId: z.string().trim().min(1, 'Employee is required'),
  attendanceDate: z.string().trim().min(1, 'Attendance date is required'),
  ...attendanceCommonSchema,
});

export const updateAttendanceSchema = z.object(attendanceCommonSchema);

export type AttendanceFormValues = z.infer<typeof createAttendanceSchema>;

/** Mirrors HMS.Modules.HR.Application.Validators.CheckInRequestValidator/CheckOutRequestValidator. */
export const checkInOutSchema = z.object({
  employeeId: z.string().trim().min(1, 'Employee is required'),
  time: z.string().trim().optional().or(z.literal('')),
});

export type CheckInOutFormValues = z.infer<typeof checkInOutSchema>;
