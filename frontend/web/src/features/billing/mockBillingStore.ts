import { summarizeBilling, toBillingItems } from './billingCalculations';
import type { BillingFormValues } from './billingValidation';
import type { Billing } from './types';

/**
 * Offline-only store for submitted billing records — there is no Billing API yet (see
 * frontend/web/src/features/patients/mockPatientsStore.ts for the same pattern used ahead
 * of the Patients API). Persisted to localStorage so a demo survives a page refresh. Swap
 * for a real `billingApi.create()` call once the backend exposes one; `saveBillingForPatient`
 * is the only call site that needs to change.
 */
const STORAGE_KEY = 'hms-mock-billing';

function loadAll(): Billing[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as Billing[];
      if (Array.isArray(parsed)) return parsed;
    }
  } catch {
    // Corrupt/unavailable storage — start fresh.
  }
  return [];
}

function persist(all: Billing[]) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(all));
  } catch {
    // Storage unavailable (e.g. private browsing quota) — demo still works for this tab session.
  }
}

let billings: Billing[] = loadAll();
let nextSeq = billings.reduce((max, b) => Math.max(max, Number(b.id.replace('mock-bill-', '')) || 0), 0) + 1;

/** Returns null (and saves nothing) when every billing category was left blank — an empty bill isn't a record worth keeping. */
export function saveBillingForPatient(patientId: string, visitId: string, values: BillingFormValues): Billing | null {
  const items = toBillingItems(values);
  if (items.length === 0) return null;

  const summary = summarizeBilling(values);
  const billing: Billing = {
    id: `mock-bill-${nextSeq++}`,
    patientId,
    visitId,
    items,
    grossAmount: summary.grossTotal,
    totalDiscount: summary.discountTotal,
    netAmount: summary.netTotal,
  };
  billings = [...billings, billing];
  persist(billings);
  return billing;
}

export function getBillingForPatient(patientId: string): Billing[] {
  return billings.filter((b) => b.patientId === patientId);
}
