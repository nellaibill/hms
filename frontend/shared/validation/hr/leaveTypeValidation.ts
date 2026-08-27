import { z } from 'zod';

/**
 * Mirrors HMS.Modules.HR.Application.Validators.CreateLeaveTypeRequestValidator /
 * UpdateLeaveTypeRequestValidator exactly — only Code (create-only, natural key, immutable
 * after creation) and Name are required; MaxDaysPerYear must be > 0 when supplied (null
 * means unlimited).
 */
const leaveTypeCommonSchema = {
  name: z.string().trim().min(1, 'Name is required').max(150),
  // Preprocessed so a blank form field (empty string, from an <input type="number">) means
  // "unlimited" (null) rather than failing coercion to 0/NaN.
  maxDaysPerYear: z.preprocess(
    (val) => (val === '' || val === undefined ? null : val),
    z.union([z.null(), z.coerce.number().int().positive('Must be greater than 0')]),
  ).optional(),
  isPaid: z.boolean(),
  isActive: z.boolean(),
};

export const createLeaveTypeSchema = z.object({
  code: z.string().trim().min(1, 'Code is required').max(30),
  ...leaveTypeCommonSchema,
});

export const updateLeaveTypeSchema = z.object(leaveTypeCommonSchema);

export type LeaveTypeFormValues = z.infer<typeof createLeaveTypeSchema>;
