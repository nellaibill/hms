import type { PatientRegistrationUiFormValues } from '@hms/shared';

/**
 * Autosaves in-progress New Patient Registration form state to localStorage so a refresh
 * (or an accidental tab close) doesn't lose data already entered on earlier tabs — this is
 * the only user-facing form long enough that losing it mid-fill is a real cost. Cleared on
 * successful submission (see PatientRegistrationCreatePage's onSuccess) so the next "New
 * Patient" starts clean rather than reopening a stale draft.
 */
const STORAGE_KEY = 'hms-patient-registration-draft';

export interface RegistrationDraft {
  values: PatientRegistrationUiFormValues;
  activeTab: string;
}

export function loadRegistrationDraft(): RegistrationDraft | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as unknown;
    if (parsed && typeof parsed === 'object' && 'values' in parsed && 'activeTab' in parsed) {
      return parsed as RegistrationDraft;
    }
  } catch {
    // Corrupt/unavailable storage — start fresh.
  }
  return null;
}

export function saveRegistrationDraft(values: PatientRegistrationUiFormValues, activeTab: string): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ values, activeTab }));
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
