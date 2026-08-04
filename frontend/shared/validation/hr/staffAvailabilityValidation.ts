import { z } from 'zod';
import { AVAILABILITY_STATUSES } from '../../enums/hr';

/**
 * Mirrors HMS.Modules.HR.Application.Validators.CreateStaffAvailabilityRequestValidator /
 * UpdateStaffAvailabilityRequestValidator exactly — StaffId, StartDate, EndDate,
 * AvailabilityStatus are required; Reason is optional. No date-order/overlap checks —
 * explicitly out of scope per the backend's own doc comment (client-side convenience only,
 * the backend remains authoritative — docs/ApiStandards.md §7, docs/FrontendArchitecture.md §9).
 */
export const staffAvailabilitySchema = z.object({
  staffId: z.string().trim().min(1, 'Staff is required'),
  startDate: z.string().trim().min(1, 'Start date is required'),
  endDate: z.string().trim().min(1, 'End date is required'),
  availabilityStatus: z.enum(AVAILABILITY_STATUSES, { message: 'Availability status is required' }),
  reason: z.string().trim().max(500).optional().or(z.literal('')),
});

export const createStaffAvailabilitySchema = staffAvailabilitySchema;
export const updateStaffAvailabilitySchema = staffAvailabilitySchema;

export type StaffAvailabilityFormValues = z.infer<typeof staffAvailabilitySchema>;
