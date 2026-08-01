import type { PatientRegistrationUiFormValues } from '@hms/shared';
import { billingFormSchema, defaultBillingFormValues, type BillingFormValues } from '../billing/billingValidation';

/**
 * Autosaves in-progress New Patient Registration form state to localStorage so a refresh
 * (or an accidental tab close) doesn't lose data already entered on earlier tabs — this is
 * the only user-facing form long enough that losing it mid-fill is a real cost. Cleared on
 * successful submission (see PatientRegistrationCreatePage's onSuccess) so the next "New
 * Patient" starts clean rather than reopening a stale draft. Billing (its own useForm — see
 * BillingStep) is included here too so a refresh mid-Billing-entry doesn't lose it either.
 */
const STORAGE_KEY = 'hms-patient-registration-draft';

export interface RegistrationDraft {
  values: PatientRegistrationUiFormValues;
  billing: BillingFormValues;
  activeTab: string;
}

export function loadRegistrationDraft(): RegistrationDraft | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as unknown;
    if (!parsed || typeof parsed !== 'object' || !('values' in parsed) || !('activeTab' in parsed)) return null;

    // The billing shape has changed before (single entries → arrays, for multi-item
    // support) and will again — a draft saved under an older shape must not be trusted
    // as-is, or a stale localStorage entry crashes the app on load instead of just
    // starting Billing fresh. Everything else in the draft (further along in a mature,
    // unchanged schema) is still trusted.
    const billingResult = billingFormSchema.safeParse((parsed as { billing?: unknown }).billing);
    return {
      ...(parsed as RegistrationDraft),
      billing: billingResult.success ? billingResult.data : defaultBillingFormValues,
    };
  } catch {
    // Corrupt/unavailable storage — start fresh.
  }
  return null;
}

export function saveRegistrationDraft(values: PatientRegistrationUiFormValues, billing: BillingFormValues, activeTab: string): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ values, billing, activeTab }));
  } catch {
    // Storage unavailable (e.g. private browsing quota) — draft just won't persist this session.
  }
}

export function clearRegistrationDraft(): void {
  try {
    localStorage.removeItem(STORAGE_KEY);
  } catch {
    // Ignore — nothing to clean up if storage was never available.
  }
}
