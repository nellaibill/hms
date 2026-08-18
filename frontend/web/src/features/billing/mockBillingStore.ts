import type { PaginationMeta } from '@hms/shared';
import { getOverallPaymentStatus, summarizeBilling, toBillingItems } from './billingCalculations';
import type { BillingFormValues } from './billingValidation';
import type { Billing, PaymentStatus } from './types';

/**
 * Offline-only fallback store — apiBillingRepository.ts is the real entry point now (calls
 * the actual Billing API, falling back to these functions only on a genuine NetworkError,
 * e.g. the static GitHub Pages deploy with no backend behind it). Nothing outside
 * apiBillingRepository.ts should import from this file directly. Persisted to localStorage
 * so the fallback survives a page refresh — mirrors mockRolesStore.ts's identical role.
 */
const STORAGE_KEY = 'hms-mock-billing';

/**
 * Seed demo invoices (mirrors features/patients/mockPatients.ts's PATIENT_SEEDS) so the
 * Unified Invoice Ledger has something to show before any registration has run in this
 * browser — spans every billing type, a mix of Paid/Pending, and a couple of discounts.
 */
const SEED_BILLINGS: Billing[] = [
  {
    id: 'mock-bill-seed-001',
    patientId: 'mock-001',
    visitId: 'mock-reg-001',
    patientName: 'Aravind Nadar',
    patientUhid: 'NH20260001',
    createdAt: '2026-07-20T09:30:00.000Z',
    items: [
      {
        id: 'consultation',
        billingType: 'Consultation',
        departmentId: 'cardiology',
        consultantId: 'dr-revathi',
        serviceId: 'new',
        quantity: 1,
        unitPrice: 720,
        discount: 0,
        discountApproved: false,
        paymentStatus: 'Paid',
        total: 720,
      },
    ],
    grossAmount: 720,
    totalDiscount: 0,
    netAmount: 720,
  },
  {
    id: 'mock-bill-seed-002',
    patientId: 'mock-002',
    visitId: 'mock-reg-002',
    patientName: 'Kavitha Pillai',
    patientUhid: 'NH20260002',
    createdAt: '2026-07-21T11:05:00.000Z',
    items: [
      {
        id: 'consultation',
        billingType: 'Consultation',
        departmentId: 'general-medicine',
        consultantId: 'dr-balasubramanian',
        serviceId: 'new',
        quantity: 1,
        unitPrice: 330,
        discount: 0,
        discountApproved: false,
        paymentStatus: 'Paid',
        total: 330,
      },
      {
        id: 'laboratory-0',
        billingType: 'Laboratory',
        consultantId: 'dr-geetha',
        serviceId: 'cbc',
        quantity: 1,
        unitPrice: 250,
        discount: 0,
        discountApproved: false,
        paymentStatus: 'Pending',
        total: 250,
      },
    ],
    grossAmount: 580,
    totalDiscount: 0,
    netAmount: 580,
  },
  {
    id: 'mock-bill-seed-003',
    patientId: 'mock-003',
    visitId: 'mock-reg-003',
    patientName: 'Praveen Iyer',
    patientUhid: 'NH20260003',
    createdAt: '2026-07-22T14:40:00.000Z',
    items: [
      {
        id: 'radiology-0',
        billingType: 'Radiology',
        consultantId: 'dr-nirmala',
        serviceId: 'ct-chest',
        quantity: 1,
        unitPrice: 3000,
        discount: 300,
        discountApproved: true,
        discountApprovedBy: 'Muthuraman Pillai',
        paymentStatus: 'Pending',
        total: 2700,
      },
    ],
    grossAmount: 3000,
    totalDiscount: 300,
    netAmount: 2700,
  },
  {
    id: 'mock-bill-seed-004',
    patientId: 'mock-004',
    visitId: 'mock-reg-004',
    patientName: 'Nandhini Thevar',
    patientUhid: 'NH20260004',
    createdAt: '2026-07-24T10:15:00.000Z',
    items: [
      {
        id: 'procedure-0',
        billingType: 'Procedure',
        consultantId: 'staff-nurse-kalaivani',
        serviceId: 'wound-dressing',
        quantity: 1,
        unitPrice: 200,
        discount: 0,
        discountApproved: false,
        paymentStatus: 'Paid',
        total: 200,
      },
      {
        id: 'procedure-1',
        billingType: 'Procedure',
        consultantId: 'staff-nurse-kalaivani',
        serviceId: 'injection',
        quantity: 1,
        unitPrice: 100,
        discount: 0,
        discountApproved: false,
        paymentStatus: 'Paid',
        total: 100,
      },
    ],
    grossAmount: 300,
    totalDiscount: 0,
    netAmount: 300,
  },
  {
    id: 'mock-bill-seed-005',
    patientId: 'mock-005',
    visitId: 'mock-reg-005',
    patientName: 'Vignesh Rajan',
    patientUhid: 'NH20260005',
    createdAt: '2026-07-27T16:20:00.000Z',
    items: [
      {
        id: 'consultation',
        billingType: 'Consultation',
        departmentId: 'orthopedics',
        consultantId: 'dr-suresh-kumar',
        serviceId: 'emergency',
        quantity: 1,
        unitPrice: 750,
        discount: 0,
        discountApproved: false,
        paymentStatus: 'Pending',
        total: 750,
      },
      {
        id: 'radiology-0',
        billingType: 'Radiology',
        consultantId: 'dr-ashok-kumar',
        serviceId: 'xray-limb',
        quantity: 1,
        unitPrice: 350,
        discount: 0,
        discountApproved: false,
        paymentStatus: 'Pending',
        total: 350,
      },
    ],
    grossAmount: 1100,
    totalDiscount: 0,
    netAmount: 1100,
  },
  {
    id: 'mock-bill-seed-006',
    patientId: 'mock-006',
    visitId: 'mock-reg-006',
    patientName: 'Keerthana Chettiar',
    patientUhid: 'NH20260006',
    createdAt: '2026-07-29T08:50:00.000Z',
    items: [
      {
        id: 'laboratory-0',
        billingType: 'Laboratory',
        consultantId: 'mr-vignesh',
        serviceId: 'thyroid-profile',
        quantity: 1,
        unitPrice: 700,
        discount: 100,
        discountApproved: true,
        discountApprovedBy: 'Dr. Priya',
        paymentStatus: 'Paid',
        total: 600,
      },
    ],
    grossAmount: 700,
    totalDiscount: 100,
    netAmount: 600,
  },
];

function loadAll(): Billing[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as Billing[];
      if (Array.isArray(parsed) && parsed.length > 0) return parsed;
    }
  } catch {
    // Corrupt/unavailable storage — fall through to seed data.
  }
  return [...SEED_BILLINGS];
}

function persist(all: Billing[]) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(all));
  } catch {
    // Storage unavailable (e.g. private browsing quota) — demo still works for this tab session.
  }
}

let billings: Billing[] = loadAll();
let nextSeq =
  billings.reduce((max, b) => Math.max(max, Number(b.id.replace('mock-bill-seed-', '').replace('mock-bill-', '')) || 0), 0) + 1;

export interface PatientDisplaySnapshot {
  name: string;
  uhid: string;
}

/** Returns null (and saves nothing) when every billing category was left blank — an empty bill isn't a record worth keeping. */
export function saveBillingForPatient(
  patientId: string,
  visitId: string,
  values: BillingFormValues,
  patient: PatientDisplaySnapshot,
): Billing | null {
  const items = toBillingItems(values);
  if (items.length === 0) return null;

  const summary = summarizeBilling(values);
  const billing: Billing = {
    id: `mock-bill-${nextSeq++}`,
    patientId,
    visitId,
    patientName: patient.name,
    patientUhid: patient.uhid,
    createdAt: new Date().toISOString(),
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

/** Unpaginated, unfiltered — for report aggregation (features/reports), which needs every invoice to compute totals/date-range filters itself rather than a single page of them. */
export function getAllMockBillings(): Billing[] {
  return billings;
}

export interface BillingListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  paymentStatus?: PaymentStatus;
}

export interface PagedBillings {
  items: Billing[];
  meta: PaginationMeta;
}

export function listMockBillings(query: BillingListQuery): PagedBillings {
  const page = query.page ?? 1;
  const pageSize = query.pageSize ?? 20;
  const search = query.search?.trim().toLowerCase();

  let items = billings;
  if (search) {
    items = items.filter(
      (b) => b.patientName.toLowerCase().includes(search) || b.patientUhid.toLowerCase().includes(search) || b.id.toLowerCase().includes(search),
    );
  }
  if (query.paymentStatus) {
    items = items.filter((b) => getOverallPaymentStatus(b.items) === query.paymentStatus);
  }

  const sort = query.sort ?? '-createdAt';
  const direction = sort.startsWith('-') ? -1 : 1;
  const field = sort.startsWith('-') ? sort.slice(1) : sort;
  items = [...items].sort((a, b) => {
    if (field === 'netAmount') return (a.netAmount - b.netAmount) * direction;
    if (field === 'patientName') return a.patientName.localeCompare(b.patientName) * direction;
    return a.createdAt.localeCompare(b.createdAt) * direction;
  });

  const totalCount = items.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const start = (page - 1) * pageSize;
  const pageItems = items.slice(start, start + pageSize);

  return {
    items: pageItems,
    meta: { page, pageSize, totalCount, totalPages },
  };
}

export function getMockBillingById(id: string): Billing | undefined {
  return billings.find((b) => b.id === id);
}

/** Marks one line item Paid — the only mutation the mock ledger supports so far (see the plan's follow-up passes for refunds/adjustments). */
export function recordMockPayment(billingId: string, itemId: string): Billing {
  const billing = getMockBillingById(billingId);
  if (!billing) {
    throw new Error(`Mock billing ${billingId} not found.`);
  }
  const updated: Billing = {
    ...billing,
    items: billing.items.map((item) => (item.id === itemId ? { ...item, paymentStatus: 'Paid' } : item)),
  };
  billings = billings.map((b) => (b.id === billingId ? updated : b));
  persist(billings);
  return updated;
}
