import type { PatientRegistrationUiFormValues } from '@hms/shared';
import { billingFormSchema, defaultBillingFormValues, type BillingFormValues } from '../billing/billingValidation';

/**
 * Autosaves in-progress New Patient Registration form state so a refresh (or an accidental
 * tab close) doesn't lose data already entered on earlier tabs — this is the only user-facing
 * form long enough that losing it mid-fill is a real cost. Cleared on successful submission
 * (see PatientRegistrationCreatePage's onSuccess) so the next "New Patient" starts clean
 * rather than reopening a stale draft. Billing (its own useForm — see BillingStep) is
 * included here too so a refresh mid-Billing-entry doesn't lose it either.
 *
 * This form captures a patient's full demographics, contact numbers, and emergency contact
 * — real PII — so the draft is handled more carefully than a typical UI convenience cache:
 *
 * - `sessionStorage`, not `localStorage`: cleared automatically when the tab/browser closes,
 *   rather than sitting on disk indefinitely across restarts.
 * - A 24-hour expiry (`savedAt`), matching the auto-purge window documented in
 *   docs/PatientRegistrationModule.md §6 — a draft from yesterday (or an abandoned session
 *   from a shared front-desk workstation) is treated as gone, not silently reloaded.
 * - Lightly obfuscated (base64), not left as raw plaintext JSON — this is NOT encryption
 *   (client-side JS can't hide a key from itself, so anyone with devtools access to this
 *   origin can still decode it) but it does mean the PII isn't sitting in cleartext for a
 *   casual glance at storage contents, and it invalidates old unobfuscated drafts on upgrade
 *   (they simply fail to parse and are treated as "no draft").
 */
const STORAGE_KEY = 'hms-patient-registration-draft';
const DRAFT_TTL_MS = 24 * 60 * 60 * 1000;

export interface RegistrationDraft {
  values: PatientRegistrationUiFormValues;
  billing: BillingFormValues;
  activeTab: string;
}

interface StoredDraft extends RegistrationDraft {
  savedAt: number;
}

function encode(json: string): string {
  return btoa(unescape(encodeURIComponent(json)));
}

function decode(raw: string): string {
  return decodeURIComponent(escape(atob(raw)));
}

export function loadRegistrationDraft(): RegistrationDraft | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(decode(raw)) as unknown;
    if (
      !parsed ||
      typeof parsed !== 'object' ||
      !('values' in parsed) ||
      !('activeTab' in parsed) ||
      !('savedAt' in parsed) ||
      typeof (parsed as { savedAt: unknown }).savedAt !== 'number'
    ) {
      return null;
    }

    const stored = parsed as StoredDraft;
    if (Date.now() - stored.savedAt > DRAFT_TTL_MS) {
      sessionStorage.removeItem(STORAGE_KEY);
      return null;
    }

    // The billing shape has changed before (single entries → arrays, for multi-item
    // support) and will again — a draft saved under an older shape must not be trusted
    // as-is, or a stale entry crashes the app on load instead of just starting Billing
    // fresh. Everything else in the draft (further along in a mature, unchanged schema) is
    // still trusted.
    const billingResult = billingFormSchema.safeParse(stored.billing);
    return {
      values: stored.values,
      activeTab: stored.activeTab,
      billing: billingResult.success ? billingResult.data : defaultBillingFormValues,
    };
  } catch {
    // Corrupt/unavailable storage, or an unobfuscated draft from before this format — start fresh.
  }
  return null;
}

export function saveRegistrationDraft(values: PatientRegistrationUiFormValues, billing: BillingFormValues, activeTab: string): void {
  try {
    const stored: StoredDraft = { values, billing, activeTab, savedAt: Date.now() };
    sessionStorage.setItem(STORAGE_KEY, encode(JSON.stringify(stored)));
  } catch {
    // Storage unavailable (e.g. private browsing quota) — draft just won't persist this session.
  }
}

export function clearRegistrationDraft(): void {
  try {
    sessionStorage.removeItem(STORAGE_KEY);
  } catch {
    // Ignore — nothing to clean up if storage was never available.
  }
}
