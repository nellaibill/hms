import { resolveRecordLabel } from '@/features/masters';
import { isConsultationEntryActive, isServiceEntryActive } from './billingActivity';
import type { BillingFormValues, ConsultationBillingFormValues, ServiceBillingCategory, ServiceBillingRowFormValues } from './billingValidation';
import type { BillingItem, BillingType, PaymentStatus } from './types';

export function formatCurrency(amount: number): string {
  return `₹${Math.round(amount).toLocaleString('en-IN')}`;
}

/** An invoice with several line items has one status only once every item is Paid — any Pending item keeps the whole invoice Pending, matching how the Ledger and its filter should read it. */
export function getOverallPaymentStatus(items: BillingItem[]): PaymentStatus {
  return items.length > 0 && items.every((item) => item.paymentStatus === 'Paid') ? 'Paid' : 'Pending';
}

export interface BillingSummaryLine {
  billingType: BillingType;
  label: string;
  charge: number;
  discount: number;
  net: number;
  /** How many active (non-empty) entries make up this line. */
  count: number;
  active: boolean;
}

export interface BillingSummary {
  lines: BillingSummaryLine[];
  grossTotal: number;
  discountTotal: number;
  netTotal: number;
}

const SERVICE_LABELS: Record<ServiceBillingCategory, string> = {
  radiology: 'Radiology',
  laboratory: 'Laboratory',
  procedure: 'Procedure',
};

const SERVICE_BILLING_TYPES: Record<ServiceBillingCategory, BillingType> = {
  radiology: 'Radiology',
  laboratory: 'Laboratory',
  procedure: 'Procedure',
};

/** Generic over any row shape carrying `charge`/`quantity`/`discount` — reused for both Consultation rows and the three Service-category row shapes, which otherwise differ (departmentId+consultationTypeId vs. serviceId). */
function summarizeRows<T extends { charge: number; quantity: number; discount: number }>(rows: T[], isActive: (row: T) => boolean) {
  const activeRows = rows.filter(isActive);
  const charge = activeRows.reduce((sum, row) => sum + row.charge * row.quantity, 0);
  const discount = activeRows.reduce((sum, row) => sum + row.discount, 0);
  return { charge, discount, count: activeRows.length };
}

/** Recomputed from current form values on every change — never cached, so it can't drift out of sync with the cards. */
export function summarizeBilling(values: BillingFormValues): BillingSummary {
  const consultationSummary = summarizeRows(values.consultation, isConsultationEntryActive);
  const lines: BillingSummaryLine[] = [
    {
      billingType: 'Consultation',
      label: 'Consultation',
      charge: consultationSummary.charge,
      discount: consultationSummary.discount,
      net: Math.max(consultationSummary.charge - consultationSummary.discount, 0),
      count: consultationSummary.count,
      active: consultationSummary.count > 0,
    },
    ...(Object.keys(SERVICE_LABELS) as ServiceBillingCategory[]).map((category) => {
      const { charge, discount, count } = summarizeRows(values[category], isServiceEntryActive);
      return {
        billingType: SERVICE_BILLING_TYPES[category],
        label: SERVICE_LABELS[category],
        charge,
        discount,
        net: Math.max(charge - discount, 0),
        count,
        active: count > 0,
      };
    }),
  ];

  const activeLines = lines.filter((line) => line.active);
  const grossTotal = activeLines.reduce((sum, line) => sum + line.charge, 0);
  const discountTotal = activeLines.reduce((sum, line) => sum + line.discount, 0);
  const netTotal = Math.max(grossTotal - discountTotal, 0);

  return { lines, grossTotal, discountTotal, netTotal };
}

function consultationRowToBillingItem(entry: ConsultationBillingFormValues, index: number, paymentStatus: PaymentStatus): BillingItem {
  return {
    id: `consultation-${index}`,
    billingType: 'Consultation',
    departmentId: entry.departmentId,
    consultantId: entry.consultantId,
    serviceId: entry.consultationTypeId,
    quantity: entry.quantity,
    unitPrice: entry.charge,
    discount: entry.discount,
    discountApproved: entry.discountApproved,
    discountApprovedBy: entry.discountApprovedBy || undefined,
    paymentStatus,
    total: Math.max(entry.quantity * entry.charge - entry.discount, 0),
  };
}

function serviceRowToBillingItem(
  category: ServiceBillingCategory,
  entry: ServiceBillingRowFormValues,
  index: number,
  paymentStatus: PaymentStatus,
): BillingItem {
  return {
    id: `${category}-${index}`,
    billingType: SERVICE_BILLING_TYPES[category],
    consultantId: entry.consultantId,
    serviceId: entry.serviceId,
    quantity: entry.quantity,
    unitPrice: entry.charge,
    discount: entry.discount,
    discountApproved: entry.discountApproved,
    discountApprovedBy: entry.discountApprovedBy || undefined,
    paymentStatus,
    total: Math.max(entry.quantity * entry.charge - entry.discount, 0),
  };
}

/**
 * Flattens the form's per-category shape into the normalized BillingItem[] model — only
 * entries actually in use produce a line, and every category (Consultation included) can
 * contribute more than one. `values.paymentStatus` is the single whole-bill status (see
 * billingValidation.ts); every produced line starts there and can only diverge later via the
 * Record Payment action on an already-saved invoice, not at creation time.
 */
export function toBillingItems(values: BillingFormValues): BillingItem[] {
  const items: BillingItem[] = [];

  values.consultation.forEach((entry, index) => {
    if (!isConsultationEntryActive(entry)) return;
    items.push(consultationRowToBillingItem(entry, index, values.paymentStatus));
  });

  (Object.keys(SERVICE_LABELS) as ServiceBillingCategory[]).forEach((category) => {
    values[category].forEach((row, index) => {
      if (!isServiceEntryActive(row)) return;
      items.push(serviceRowToBillingItem(category, row, index, values.paymentStatus));
    });
  });

  return items;
}

export interface BillingItemDescription {
  serviceLabel: string;
  consultantName: string;
}

/**
 * A saved BillingItem only stores Masters ids (department/service/consultant) — this resolves
 * them back to display names for read-only views like the patient detail page, via the
 * Masters engine's reference cache (registry.ts's resolveRecordLabel) — the same synchronous
 * id→label lookup reference fields use elsewhere. That cache is populated by a
 * `useMasterOptionsQuery(entityKey)` call somewhere in the calling page (see
 * LaboratoryBillingCard/ConsultationBillingCard/InvoiceDetailCard/BillingSummaryCard/
 * PatientBillingTab) — falls back to the raw id until that query resolves, then self-corrects
 * on the next render.
 */
export function describeBillingItem(item: BillingItem): BillingItemDescription {
  if (item.billingType === 'Consultation') {
    const departmentLabel = item.departmentId ? resolveRecordLabel('department', item.departmentId) : undefined;
    const typeLabel = item.serviceId ? resolveRecordLabel('consultationType', item.serviceId) : undefined;
    return {
      serviceLabel: [departmentLabel, typeLabel].filter(Boolean).join(' — ') || 'Consultation',
      consultantName: item.consultantId ? resolveRecordLabel('consultant', item.consultantId) : '—',
    };
  }

  if (item.billingType === 'Pharmacy') {
    // Generated server-side only (DispenseService's best-effort billing step, ADR-028) — its
    // serviceId is already the full human-readable description ("Paracetamol 500mg (Batch
    // B-2026-001) × 4"), not a catalog id to resolve, and there's no consultant on a dispense.
    return { serviceLabel: item.serviceId ?? 'Pharmacy', consultantName: '—' };
  }

  // Radiology/Laboratory/Procedure: serviceId is a DiagnosticTest id, consultantId a real Consultant id.
  return {
    serviceLabel: item.serviceId ? resolveRecordLabel('diagnosticTest', item.serviceId) : item.billingType,
    consultantName: item.consultantId ? resolveRecordLabel('consultant', item.consultantId) : '—',
  };
}
