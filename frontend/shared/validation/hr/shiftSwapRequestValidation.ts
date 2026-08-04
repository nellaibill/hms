import { z } from 'zod';
import { SWAP_REQUEST_STATUSES } from '../../enums/hr';

/**
 * Mirrors HMS.Modules.HR.Application.Validators.CreateSwapRequestValidator /
 * UpdateSwapRequestValidator exactly — RequestedByStaffId, RequestedToStaffId,
 * CurrentShiftAssignmentId, RequestedShiftAssignmentId, Status, RequestedDate are required;
 * ApprovedDate/ApprovedBy/Remarks are optional. No approval workflow validation, no conflict
 * detection — explicitly out of scope per the backend's own doc comment (client-side
 * convenience only, the backend remains authoritative — docs/ApiStandards.md §7,
 * docs/FrontendArchitecture.md §9).
 */
export const swapRequestSchema = z.object({
  requestedByStaffId: z.string().trim().min(1, 'Requesting staff is required'),
  requestedToStaffId: z.string().trim().min(1, 'Requested-to staff is required'),
  currentShiftAssignmentId: z.string().trim().min(1, 'Current shift assignment is required'),
  requestedShiftAssignmentId: z.string().trim().min(1, 'Requested shift assignment is required'),
  status: z.enum(SWAP_REQUEST_STATUSES, { message: 'Status is required' }),
  requestedDate: z.string().trim().min(1, 'Requested date is required'),
  approvedDate: z.string().trim().optional().or(z.literal('')),
  approvedBy: z.string().trim().optional().or(z.literal('')),
  remarks: z.string().trim().max(500).optional().or(z.literal('')),
});

export const createSwapRequestSchema = swapRequestSchema;
export const updateSwapRequestSchema = swapRequestSchema;

export type SwapRequestFormValues = z.infer<typeof swapRequestSchema>;
