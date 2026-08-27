import { z } from 'zod';

/**
 * Mirrors HMS.Modules.HR.Application.Validators.CreateLeaveRequestRequestValidator —
 * EmployeeId/LeaveTypeId/StartDate/EndDate/Reason are all required, and EndDate must not be
 * earlier than StartDate.
 */
export const createLeaveRequestSchema = z
  .object({
    employeeId: z.string().trim().min(1, 'Employee is required'),
    leaveTypeId: z.string().trim().min(1, 'Leave type is required'),
    startDate: z.string().trim().min(1, 'Start date is required'),
    endDate: z.string().trim().min(1, 'End date is required'),
    reason: z.string().trim().min(1, 'Reason is required').max(500),
  })
  .refine((values) => !values.startDate || !values.endDate || values.endDate >= values.startDate, {
    message: 'End date must not be earlier than start date.',
    path: ['endDate'],
  });

export type LeaveRequestFormValues = z.infer<typeof createLeaveRequestSchema>;

/** Mirrors HMS.Modules.HR.Application.Validators.ApproveLeaveRequestRequestValidator. */
export const approveLeaveRequestSchema = z.object({
  notes: z.string().trim().max(500).optional().or(z.literal('')),
});

export type ApproveLeaveRequestFormValues = z.infer<typeof approveLeaveRequestSchema>;

/** Mirrors HMS.Modules.HR.Application.Validators.RejectLeaveRequestRequestValidator — a
 * rejection reason is required. */
export const rejectLeaveRequestSchema = z.object({
  reason: z.string().trim().min(1, 'A reason is required').max(500),
});

export type RejectLeaveRequestFormValues = z.infer<typeof rejectLeaveRequestSchema>;
