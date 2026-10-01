import { z } from 'zod';

/**
 * Mirrors HMS.Shared.Kernel.BloodPressureFormat — the one rule for a blood pressure typed as free
 * text ("120/80"), shared by the OPD consultation and discharge summary vitals. Ranges match IPD's
 * numeric vitals (systolic 40–300, diastolic 20–200); systolic must be higher than diastolic so a
 * transposed reading is caught, and a trailing "mmHg" is tolerated. Client-side convenience only,
 * the backend remains authoritative (docs/ApiStandards.md §7, docs/FrontendArchitecture.md §9).
 */
export const BLOOD_PRESSURE_MESSAGE =
  'Blood pressure must be systolic/diastolic, e.g. 120/80 (systolic 40–300, diastolic 20–200, systolic higher than diastolic).';

const BLOOD_PRESSURE_PATTERN = /^\s*(\d{2,3})\s*\/\s*(\d{2,3})\s*(mm\s*hg)?\s*$/i;

export function isValidBloodPressureOrEmpty(value: string | undefined | null): boolean {
  if (!value || !value.trim()) return true;
  const match = BLOOD_PRESSURE_PATTERN.exec(value);
  if (!match) return false;
  const systolic = Number(match[1]);
  const diastolic = Number(match[2]);
  return systolic >= 40 && systolic <= 300 && diastolic >= 20 && diastolic <= 200 && systolic > diastolic;
}

/** Optional free-text BP field: blank is fine, anything else must be a plausible reading. */
export const optionalBloodPressureSchema = z
  .string()
  .trim()
  .max(20)
  .refine(isValidBloodPressureOrEmpty, BLOOD_PRESSURE_MESSAGE)
  .optional()
  .or(z.literal(''));
