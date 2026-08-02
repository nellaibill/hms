import { isConsultationEntryActive, isServiceEntryActive } from './billingActivity';
import {
  CONSULTATION_CONSULTANTS,
  CONSULTATION_DEPARTMENTS,
  CONSULTATION_TYPES,
  LABORATORY_CONSULTANTS,
  LABORATORY_SERVICES,
  PROCEDURE_CONSULTANTS,
  PROCEDURE_SERVICES,
  RADIOLOGY_CONSULTANTS,
  RADIOLOGY_SERVICES,
} from './billingCatalog';
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
  /** How many active (non-empty) entries make up this line — mainly meaningful for the array-based service categories. */
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

function summarizeServiceRows(rows: ServiceBillingRowFormValues[]) {
  const activeRows = rows.filter(isServiceEntryActive);
  const charge = activeRows.reduce((sum, row) => sum + row.charge, 0);
  const discount = activeRows.reduce((sum, row) => sum + row.discount, 0);
  return { charge, discount, count: activeRows.length };
}

/** Recomputed from current form values on every change — never cached, so it can't drift out of sync with the cards. */
export function summarizeBilling(values: BillingFormValues): BillingSummary {
  const consultationActive = isConsultationEntryActive(values.consultation);
  const lines: BillingSummaryLine[] = [
    {
      billingType: 'Consultation',
      label: 'Consultation',
      charge: values.consultation.charge,
      discount: values.consultation.discount,
      net: Math.max(values.consultation.charge - values.consultation.discount, 0),
      count: consultationActive ? 1 : 0,
      active: consultationActive,
    },
    ...(Object.keys(SERVICE_LABELS) as ServiceBillingCategory[]).map((category) => {
      const { charge, discount, count } = summarizeServiceRows(values[category]);
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

function consultationToBillingItem(entry: ConsultationBillingFormValues): BillingItem {
  return {
    id: 'consultation',
    billingType: 'Consultation',
    departmentId: entry.departmentId,
    consultantId: entry.consultantId,
    serviceId: entry.consultationTypeId,
    quantity: 1,
    unitPrice: entry.charge,
    discount: entry.discount,
    discountApproved: entry.discountApproved,
    discountApprovedBy: entry.discountApprovedBy || undefined,
    paymentStatus: (entry.paymentStatus || 'Pending') as PaymentStatus,
    total: Math.max(entry.charge - entry.discount, 0),
  };
}

function serviceRowToBillingItem(category: ServiceBillingCategory, entry: ServiceBillingRowFormValues, index: number): BillingItem {
  return {
    id: `${category}-${index}`,
    billingType: SERVICE_BILLING_TYPES[category],
    consultantId: entry.consultantId,
    serviceId: entry.serviceId,
    quantity: 1,
    unitPrice: entry.charge,
    discount: entry.discount,
    discountApproved: entry.discountApproved,
    discountApprovedBy: entry.discountApprovedBy || undefined,
    paymentStatus: (entry.paymentStatus || 'Pending') as PaymentStatus,
    total: Math.max(entry.charge - entry.discount, 0),
  };
}

/** Flattens the form's per-category shape into the normalized BillingItem[] model — only entries actually in use produce a line, and a service category can contribute more than one. */
export function toBillingItems(values: BillingFormValues): BillingItem[] {
  const items: BillingItem[] = [];

  if (isConsultationEntryActive(values.consultation)) {
    items.push(consultationToBillingItem(values.consultation));
  }

  (Object.keys(SERVICE_LABELS) as ServiceBillingCategory[]).forEach((category) => {
    values[category].forEach((row, index) => {
      if (!isServiceEntryActive(row)) return;
      items.push(serviceRowToBillingItem(category, row, index));
    });
  });

  return items;
}

const SERVICE_CATALOGS = {
  Radiology: RADIOLOGY_SERVICES,
  Laboratory: LABORATORY_SERVICES,
  Procedure: PROCEDURE_SERVICES,
} as const;

const SERVICE_CONSULTANT_CATALOGS = {
  Radiology: RADIOLOGY_CONSULTANTS,
  Laboratory: LABORATORY_CONSULTANTS,
  Procedure: PROCEDURE_CONSULTANTS,
} as const;

export interface BillingItemDescription {
  serviceLabel: string;
  consultantName: string;
}

/** A saved BillingItem only stores catalog IDs (department/service/consultant) — this resolves them back to display names using the same catalogs the Billing step itself reads from, for read-only views like the patient detail page. */
export function describeBillingItem(item: BillingItem): BillingItemDescription {
  if (item.billingType === 'Consultation') {
    const department = CONSULTATION_DEPARTMENTS.find((d) => d.id === item.departmentId);
    const type = CONSULTATION_TYPES.find((t) => t.id === item.serviceId);
    const consultant = CONSULTATION_CONSULTANTS.find((c) => c.id === item.consultantId);
    return {
      serviceLabel: [department?.name, type?.name].filter(Boolean).join(' — ') || 'Consultation',
      consultantName: consultant?.name ?? '—',
    };
  }

  const service = SERVICE_CATALOGS[item.billingType].find((s) => s.id === item.serviceId);
  const consultant = SERVICE_CONSULTANT_CATALOGS[item.billingType].find((c) => c.id === item.consultantId);
  return {
    serviceLabel: service?.name ?? item.billingType,
    consultantName: consultant?.name ?? '—',
  };
}
