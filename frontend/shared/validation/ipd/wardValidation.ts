import { z } from 'zod';
import { WARD_TYPES } from '../../enums/ipd';

/**
 * Mirrors HMS.Modules.IPD.Application.Validators.CreateWardRequestValidator /
 * UpdateWardRequestValidator exactly (client-side convenience only, the backend remains
 * authoritative — docs/ApiStandards.md §7, docs/FrontendArchitecture.md §9).
 */
const wardCommonSchema = {
  name: z.string().trim().min(1, 'Name is required').max(150),
  departmentId: z.string().trim().min(1, 'Department is required'),
  wardType: z.enum(WARD_TYPES, { message: 'Ward type is required' }),
  isActive: z.boolean(),
};

export const createWardSchema = z.object({
  code: z.string().trim().min(1, 'Code is required').max(30),
  ...wardCommonSchema,
});

export const updateWardSchema = z.object(wardCommonSchema);

export type WardFormValues = z.infer<typeof createWardSchema>;
