import { ADMISSION_TYPES, REFERRAL_COLUMN_CATEGORIES, recordVisitUiSchema, type EncounterTypeUi, type VisitCategory } from '@hms/shared';
import { z } from 'zod';

export const REFERRAL_ANSWERS = ['Yes', 'No'] as const;

/** Placeholder list — the hospital's real categories aren't decided yet ("A, B, C as of now").
 * Matches the backend's VisitCategory enum exactly. */
export const VISIT_CATEGORIES = ['A', 'B', 'C'] as const satisfies readonly VisitCategory[];

/** Every encounter type except OP gets the admission fields below — OP's form is unchanged. */
export function hasAdmissionDetails(encounterType: EncounterTypeUi): boolean {
  return encounterType !== 'OP';
}

/** The MLC/NMLC dropdown is one field with two names: an IP or Emergency patient is being
 * admitted, a Day Care or Observation patient is only being observed. */
export function admissionTypeLabel(encounterType: EncounterTypeUi): string {
  return encounterType === 'IP' || encounterType === 'Emergency' ? 'Admission type' : 'Observation type';
}

/**
 * The standalone "Add Visit" form's own schema: recordVisitUiSchema's core fields plus the
 * admission fields shown for IP/Day Care/Observation/Emergency, which are saved with the visit
 * (see bridging.ts's toRecordVisitRequest). Stricter than the API on purpose: the API leaves
 * Admission/Observation type optional because the New Patient Registration wizard records
 * non-OP visits without these fields (its own admissionType/referral/category are still
 * UI-only, see registrationDetailsUiSchema) — this form is where they're actually collected, so
 * this is where the type is made mandatory.
 */
export const recordVisitFormSchema = recordVisitUiSchema
  .extend({
    admissionType: z.enum(ADMISSION_TYPES).optional().or(z.literal('')),
    /** Accident Register number — asked for, and mandatory, only when admissionType is MLC. */
    arNumber: z.string().trim().max(50, 'A.R number must be 50 characters or fewer').optional().or(z.literal('')),
    referral: z.enum(REFERRAL_ANSWERS).optional().or(z.literal('')),
    /** Who referred the patient — only asked for when referral is Yes. */
    referredBy: z.enum(REFERRAL_COLUMN_CATEGORIES).optional().or(z.literal('')),
    category: z.enum(VISIT_CATEGORIES).optional().or(z.literal('')),
  })
  .superRefine((data, ctx) => {
    // Mandatory under either of its names — stricter than the wizard's registrationDetailsUiSchema,
    // which only requires it for IP and Emergency.
    if (hasAdmissionDetails(data.encounterType) && !data.admissionType) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['admissionType'],
        message: `${admissionTypeLabel(data.encounterType)} is required`,
      });
    }
    if (data.admissionType === 'MLC' && !data.arNumber) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['arNumber'], message: 'A.R number is required for MLC' });
    }
    if (data.referral === 'Yes' && !data.referredBy) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['referredBy'], message: 'Select who referred the patient' });
    }
  });

export type RecordVisitFormValues = z.infer<typeof recordVisitFormSchema>;
