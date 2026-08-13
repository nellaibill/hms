import { z } from 'zod';
import { BED_STATUSES } from '../../enums/ipd';

/**
 * Mirrors HMS.Modules.IPD.Application.Validators.CreateBedRequestValidator /
 * UpdateBedRequestValidator exactly (client-side convenience only, the backend remains
 * authoritative — docs/ApiStandards.md §7, docs/FrontendArchitecture.md §9).
 */
const bedCommonSchema = {
  bedType: z.string().trim().min(1, 'Bed type is required').max(50),
  status: z.enum(BED_STATUSES, { message: 'Status is required' }),
  isActive: z.boolean(),
  dailyCharge: z.coerce.number({ message: 'Daily charge is required' }).positive('Daily charge must be greater than 0'),
};

export const createBedSchema = z.object({
  wardId: z.string().trim().min(1, 'Ward is required'),
  bedNumber: z.string().trim().min(1, 'Bed number is required').max(30),
  ...bedCommonSchema,
});

export const updateBedSchema = z.object(bedCommonSchema);

export type BedFormValues = z.infer<typeof createBedSchema>;
