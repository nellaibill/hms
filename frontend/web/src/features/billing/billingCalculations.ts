import { resolveDiagnosticPackageLabel, resolveDiagnosticServiceLabel } from '@/features/diagnostics';
import { resolveRecordLabel } from '@/features/masters';
import { isConsultationEntryActive, isLaboratoryEntryActive, isServiceEntryActive, isSimpleServiceEntryActive } from './billingActivity';
import type {
  BillingFormValues,
  ConsultationBillingFormValues,
  LaboratoryBillingRowFormValues,
  ServiceBillingCategory,
  ServiceBillingRowFormValues,
  SimpleServiceBillingCategory,
  SimpleServiceBillingRowFormValues,
} from './billingValidation';
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

/** Radiology/Procedure only now — Laboratory forked to its own row shape/schema
 * (billingValidation.ts's laboratoryBillingSchema), handled explicitly below. */
const SERVICE_LABELS: Record<ServiceBillingCategory, string> = {
  radiology: 'Radiology',
  procedure: 'Procedure',
};

const SERVICE_BILLING_TYPES: Record<ServiceBillingCategory, BillingType> = {
  radiology: 'Radiology',
  procedure: 'Procedure',
};

/** Injection/File — same idea as SERVICE_LABELS/SERVICE_BILLING_TYPES above, for the two
 * no-consultant categories (simpleServiceBillingSchema). */
const SIMPLE_SERVICE_LABELS: Record<SimpleServiceBillingCategory, string> = {
  injection: 'Injection',
  file: 'File',
};

const SIMPLE_SERVICE_BILLING_TYPES: Record<SimpleServiceBillingCategory, BillingType> = {
  injection: 'Injection',
  file: 'File',
};

/** Generic over any row shape carrying `charge`/`quantity`/`discount` — reused for both Consultation rows and the three Service-category row shapes, which otherwise differ (departmentId+consultationTypeId vs. serviceId). */
function summarizeRows<T extends { charge: number; quantity: number; discount: number }>(rows: T[], isActive: (row: T) => boolean) {
  const activeRows = rows.filter(isActive);
  const charge = activeRows.reduce((sum, row) => sum + row.charge * row.quantity, 0);
  const discount = activeRows.reduce((sum, row) => sum + row.discount, 0);
  return { charge, discount, count: activeRows.length };
}

/** Turns a {charge, discount, count} summary into a full BillingSummaryLine. */
function toSummaryLine(billingType: BillingType, label: string, summary: { charge: number; discount: number; count: number }): BillingSummaryLine {
  return {
    billingType,
    label,
    charge: summary.charge,
    discount: summary.discount,
    net: Math.max(summary.charge - summary.discount, 0),
    count: summary.count,
    active: summary.count > 0,
  };
}

/** Recomputed from current form values on every change — never cached, so it can't drift out of sync with the cards. */
export function summarizeBilling(values: BillingFormValues): BillingSummary {
  // Explicit Consultation/Radiology/Laboratory/Procedure order (matches the cards) — Laboratory
  // sits between Radiology and Procedure here even though it's summarized separately from the
  // SERVICE_LABELS-driven categories below, since its row shape (itemType/itemId) differs.
  const lines: BillingSummaryLine[] = [
    toSummaryLine('Consultation', 'Consultation', summarizeRows(values.consultation, isConsultationEntryActive)),
    toSummaryLine('Radiology', SERVICE_LABELS.radiology, summarizeRows(values.radiology, isServiceEntryActive)),
    toSummaryLine('Laboratory', 'Laboratory', summarizeRows(values.laboratory, isLaboratoryEntryActive)),
    toSummaryLine('Procedure', SERVICE_LABELS.procedure, summarizeRows(values.procedure, isServiceEntryActive)),
    toSummaryLine('Injection', SIMPLE_SERVICE_LABELS.injection, summarizeRows(values.injection, isSimpleServiceEntryActive)),
    toSummaryLine('File', SIMPLE_SERVICE_LABELS.file, summarizeRows(values.file, isSimpleServiceEntryActive)),
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

function simpleServiceRowToBillingItem(
  category: SimpleServiceBillingCategory,
  entry: SimpleServiceBillingRowFormValues,
  index: number,
  paymentStatus: PaymentStatus,
): BillingItem {
  return {
    id: `${category}-${index}`,
    billingType: SIMPLE_SERVICE_BILLING_TYPES[category],
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
 * Laboratory's own row -> BillingItem mapping — this is where a row's itemType/itemId splits
 * into serviceId/packageId for the API (see apiBillingRepository.ts's toCreateRequest, which
 * passes both straight through to CreateInvoiceLineItemRequest): a 'service' row sets serviceId
 * and leaves packageId undefined (unchanged from how Laboratory always worked); a 'package' row
 * sets packageId and leaves serviceId undefined — the two are never both set on one row, so
 * every other place that already reads BillingItem.serviceId (describeBillingItem's Pharmacy
 * case, PatientDetails, etc.) keeps working unmodified for plain-service Laboratory lines.
 */
function laboratoryRowToBillingItem(entry: LaboratoryBillingRowFormValues, index: number, paymentStatus: PaymentStatus): BillingItem {
  return {
    id: `laboratory-${index}`,
    billingType: 'Laboratory',
    consultantId: entry.consultantId,
    serviceId: entry.itemType === 'service' ? entry.itemId : undefined,
    packageId: entry.itemType === 'package' ? entry.itemId : undefined,
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
 * contribute more than one. Every produced line starts Pending here — the form itself requires
 * full payment before save (see billingValidation.ts's `payments`), but marking every item Paid
 * is something InvoiceService applies server-side once the save actually succeeds, not
 * something this pre-save preview does.
 */
export function toBillingItems(values: BillingFormValues): BillingItem[] {
  const items: BillingItem[] = [];

  values.consultation.forEach((entry, index) => {
    if (!isConsultationEntryActive(entry)) return;
    items.push(consultationRowToBillingItem(entry, index, 'Pending'));
  });

  (Object.keys(SERVICE_LABELS) as ServiceBillingCategory[]).forEach((category) => {
    values[category].forEach((row, index) => {
      if (!isServiceEntryActive(row)) return;
      items.push(serviceRowToBillingItem(category, row, index, 'Pending'));
    });
  });

  values.laboratory.forEach((row, index) => {
    if (!isLaboratoryEntryActive(row)) return;
    items.push(laboratoryRowToBillingItem(row, index, 'Pending'));
  });

  (Object.keys(SIMPLE_SERVICE_LABELS) as SimpleServiceBillingCategory[]).forEach((category) => {
    values[category].forEach((row, index) => {
      if (!isSimpleServiceEntryActive(row)) return;
      items.push(simpleServiceRowToBillingItem(category, row, index, 'Pending'));
    });
  });

  return items;
}

export interface BillingItemDescription {
  serviceLabel: string;
  consultantName: string;
}

/**
 * A saved BillingItem only stores Masters/diagnostics ids (department/service/package/
 * consultant) — this resolves them back to display names for read-only views like the patient
 * detail page, via one of two synchronous id->label caches: the Masters engine's
 * (registry.ts's resolveRecordLabel, for Consultation/Procedure, which still read the old
 * DiagnosticTest catalog) or the standalone diagnostics one (referenceCache.ts's
 * resolveDiagnosticServiceLabel/resolveDiagnosticPackageLabel, for Radiology/Laboratory, which
 * now read the new typed DiagnosticService/DiagnosticPackage catalogs). Both are populated by a
 * priming query call somewhere in the calling page (see LaboratoryBillingCard/
 * RadiologyBillingCard/ConsultationBillingCard/InvoiceDetailCard/BillingSummaryCard/
 * PatientDetails) — falls back to the raw id until that query resolves, then self-corrects on
 * the next render.
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

  const consultantName = item.consultantId ? resolveRecordLabel('consultant', item.consultantId) : '—';

  if (item.billingType === 'Radiology' || item.billingType === 'Laboratory') {
    // Both now read the new typed DiagnosticService catalog (Radiology's data source swapped
    // from the old DiagnosticTest master — see hooks/useDiagnosticServices.ts). Laboratory can
    // additionally be a package line (packageId set, serviceId unset — see
    // laboratoryRowToBillingItem above).
    if (item.billingType === 'Laboratory' && item.packageId) {
      return { serviceLabel: resolveDiagnosticPackageLabel(item.packageId), consultantName };
    }
    return { serviceLabel: item.serviceId ? resolveDiagnosticServiceLabel(item.serviceId) : item.billingType, consultantName };
  }

  // Procedure/Injection/File: serviceId is a DiagnosticTest id — Injection/File never have a
  // consultantId (no doctor involved), so consultantName above is always '—' for these two.
  return {
    serviceLabel: item.serviceId ? resolveRecordLabel('diagnosticTest', item.serviceId) : item.billingType,
    consultantName,
  };
}
