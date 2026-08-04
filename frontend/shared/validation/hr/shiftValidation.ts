import { z } from 'zod';

/**
 * Mirrors HMS.Modules.HR.Application.Validators.CreateShiftRequestValidator /
 * UpdateShiftRequestValidator exactly — only Code (create-only), Name, StartTime, EndTime
 * are required; BreakMinutes/GraceMinutes/IsNightShift/IsActive have no backend rule beyond
 * their type, so no extra constraint is added here (client-side convenience only, the
 * backend remains authoritative — docs/ApiStandards.md §7, docs/FrontendArchitecture.md §9).
 */
const shiftCommonSchema = {
  name: z.string().trim().min(1, 'Name is required').max(150),
  startTime: z.string().trim().min(1, 'Start time is required'),
  endTime: z.string().trim().min(1, 'End time is required'),
  breakMinutes: z.coerce.number().int(),
  graceMinutes: z.coerce.number().int(),
  isNightShift: z.boolean(),
  isActive: z.boolean(),
};

export const createShiftSchema = z.object({
  code: z.string().trim().min(1, 'Code is required').max(30),
  ...shiftCommonSchema,
});

export const updateShiftSchema = z.object(shiftCommonSchema);

export type ShiftFormValues = z.infer<typeof createShiftSchema>;
