import { z } from 'zod';
import { ASSIGNMENT_STATUSES } from '../../enums/hr';

/**
 * Mirrors HMS.Modules.HR.Application.Validators.CreateShiftAssignmentRequestValidator /
 * UpdateShiftAssignmentRequestValidator — only StaffId, DepartmentId, ShiftId, RosterDate
 * are required (no overlap/leave/holiday/weekly-off checks; explicitly out of scope per the
 * backend's own doc comment). DepartmentId has no backing directory yet, so it's a plain
 * GUID-format text field, same treatment as WeeklyRoster's DepartmentId (client-side
 * convenience only, the backend remains authoritative — docs/ApiStandards.md §7,
 * docs/FrontendArchitecture.md §9).
 */
const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export const shiftAssignmentSchema = z.object({
  staffId: z.string().trim().min(1, 'Staff is required'),
  departmentId: z.string().trim().min(1, 'Department ID is required').regex(guidPattern, 'Enter a valid Department ID (GUID)'),
  shiftId: z.string().trim().min(1, 'Shift is required'),
  rosterDate: z.string().trim().min(1, 'Roster date is required'),
  status: z.enum(ASSIGNMENT_STATUSES, { message: 'Status is required' }),
  remarks: z.string().trim().max(500).optional().or(z.literal('')),
});

export const createShiftAssignmentSchema = shiftAssignmentSchema;
export const updateShiftAssignmentSchema = shiftAssignmentSchema;

export type ShiftAssignmentFormValues = z.infer<typeof shiftAssignmentSchema>;
