import { z } from 'zod';
import { FOOD_INSTRUCTIONS } from '../../enums/dischargeSummary';
import { optionalBloodPressureSchema } from '../bloodPressure';

/**
 * Mirrors HMS.Modules.DischargeSummary.Application.Validators.UpdateDischargeSummaryRequestValidator
 * exactly (client-side convenience only, the backend remains authoritative —
 * docs/ApiStandards.md §7, docs/FrontendArchitecture.md §9). Every clinical/examination/
 * vitals/course/surgical/advice field is optional (a doctor fills sections in over time
 * before Finalize), so nothing here uses `min(1)`.
 */
const optionalTrimmedString = (max: number) => z.string().trim().max(max).optional().or(z.literal(''));

/** Empty-string, undefined, or NaN (from a number input's raw text value on a blank field)
 * all mean "not entered" for an optional numeric vitals field — mirrors the pattern used by
 * frontend/shared/validation/products/productValidation.ts's optionalNonNegativeDecimal. */
function optionalBoundedNumber(min: number, max: number) {
  const message = `Must be between ${min} and ${max}`;
  return z.preprocess(
    (value) => (value === '' || value === undefined || value === null || (typeof value === 'number' && Number.isNaN(value)) ? undefined : value),
    z.coerce.number().min(min, message).max(max, message).optional(),
  );
}

/**
 * Mirrors DischargeSummaryContracts.DischargeMedicationRequestValidator. DrugName/Dose/Route
 * are the only required fields on a line — the four per-time-of-day quantities and
 * DurationDays default to 0 when omitted, a legitimate (if unusual) line, so they're bounded
 * to non-negative rather than required.
 */
export const dischargeMedicationSchema = z.object({
  sortOrder: z.coerce.number().int().min(0),
  drugName: z.string().trim().min(1, 'Drug name is required').max(200),
  dose: z.string().trim().min(1, 'Dose is required').max(100),
  route: z.string().trim().min(1, 'Route is required').max(100),
  morningQty: z.coerce.number().min(0, 'Must be zero or greater'),
  noonQty: z.coerce.number().min(0, 'Must be zero or greater'),
  eveningQty: z.coerce.number().min(0, 'Must be zero or greater'),
  nightQty: z.coerce.number().min(0, 'Must be zero or greater'),
  durationDays: z.coerce.number().int().min(0, 'Must be zero or greater'),
  foodInstruction: z.enum(FOOD_INSTRUCTIONS, { message: 'Food instruction is required' }),
});

export type DischargeMedicationFormValues = z.infer<typeof dischargeMedicationSchema>;

export const updateDischargeSummarySchema = z.object({
  finalDiagnosis: optionalTrimmedString(2000),

  chiefComplaints: optionalTrimmedString(2000),
  historyOfPresentingIllness: optionalTrimmedString(8000),
  pastMedicalHistory: optionalTrimmedString(2000),
  pastSurgicalHistory: optionalTrimmedString(2000),
  familyHistory: optionalTrimmedString(2000),
  personalHistory: optionalTrimmedString(2000),

  generalExamination: optionalTrimmedString(2000),
  cvsFindings: optionalTrimmedString(1000),
  rsFindings: optionalTrimmedString(1000),
  paFindings: optionalTrimmedString(1000),
  cnsFindings: optionalTrimmedString(1000),
  localExamination: optionalTrimmedString(2000),
  gait: optionalTrimmedString(500),

  heightCm: optionalBoundedNumber(0, 300),
  weightKg: optionalBoundedNumber(0, 500),
  pulseRate: optionalBoundedNumber(0, 300),
  respiratoryRate: optionalBoundedNumber(0, 150),
  temperatureF: optionalBoundedNumber(70, 115),
  spO2Percent: optionalBoundedNumber(0, 100),
  bloodPressure: optionalBloodPressureSchema,

  courseInHospital: optionalTrimmedString(8000),

  procedureName: optionalTrimmedString(500),
  procedureDateTime: z.string().trim().optional().or(z.literal('')),
  primarySurgeon: optionalTrimmedString(500),
  assistantSurgeons: optionalTrimmedString(1000),
  anaesthetist: optionalTrimmedString(500),
  anaesthesia: optionalTrimmedString(500),
  surgicalPosition: optionalTrimmedString(500),
  intraOperativeFindings: optionalTrimmedString(8000),
  operativeNotes: optionalTrimmedString(8000),

  diet: optionalTrimmedString(2000),
  woundCare: optionalTrimmedString(2000),
  activity: optionalTrimmedString(2000),
  physiotherapy: optionalTrimmedString(2000),
  reviewInstructions: optionalTrimmedString(2000),
  emergencyInstructions: optionalTrimmedString(2000),
  conditionAtDischarge: optionalTrimmedString(2000),

  medications: z.array(dischargeMedicationSchema),
});

export type DischargeSummaryFormValues = z.infer<typeof updateDischargeSummarySchema>;

/**
 * Mirrors DischargeSummaryContracts.FinalizeDischargeSummaryRequestValidator — every field is
 * optional (a hospital that only ever fills in "Prepared By" still gets a valid, finalized
 * summary), so this schema exists mainly so the Finalization picker form has a typed shape.
 */
export const finalizeDischargeSummarySchema = z.object({
  preparedByUserId: z.string().trim().optional().or(z.literal('')),
  checkedByUserId: z.string().trim().optional().or(z.literal('')),
  consultantApprovedByUserId: z.string().trim().optional().or(z.literal('')),
});

export type FinalizeDischargeSummaryFormValues = z.infer<typeof finalizeDischargeSummarySchema>;
